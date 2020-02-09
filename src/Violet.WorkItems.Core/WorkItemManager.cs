using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Violet.WorkItems.Provider;
using Violet.WorkItems.Types;
using Violet.WorkItems.Validation;

namespace Violet.WorkItems
{
    public class WorkItemManager
    {
        public IDataProvider DataProvider { get; set; }
        private bool _initialized = false;
        public DescriptorManager DescriptorManager { get; }
        public ValidationManager ValidationManager { get; }

        public static readonly string EmptyValue = string.Empty;

        public WorkItemManager(IDataProvider dataProvider, IDescriptorProvider descriptorProvider)
        {
            DataProvider = dataProvider;
            DescriptorManager = new DescriptorManager(descriptorProvider);
            ValidationManager = new ValidationManager(DescriptorManager);
        }

        private async Task InitAsync()
        {
            if (!_initialized) //TODO: make it thread-safe
            {
                await DescriptorManager.LoadAllAsync();

                _initialized = true;
            }
        }

        public async Task<WorkItem> CreateTemplateAsync(string projectCode, string workItemType)
        {
            if (string.IsNullOrWhiteSpace(projectCode))
            {
                throw new ArgumentException("message", nameof(projectCode));
            }

            if (string.IsNullOrWhiteSpace(workItemType))
            {
                throw new ArgumentException("message", nameof(workItemType));
            }

            await InitAsync();

            IEnumerable<Property> properties;

            var (success, propertyDescriptors) = DescriptorManager.GetAllPropertyDescriptors(workItemType);

            if (success)
            {
                properties = propertyDescriptors.Select(pd => new Property(pd.Name, pd.DataType, pd.InitialValue ?? EmptyValue));
            }
            else
            {
                properties = new List<Property>();
            }

            var wi = new WorkItem(projectCode, "NEW", workItemType, properties, new List<LogEntry>());

            return wi;
        }

        public async Task<WorkItemCreatedResult> CreateAsync(string projectCode, string workItemType, IEnumerable<Property> properties)
        {
            if (string.IsNullOrWhiteSpace(projectCode))
            {
                throw new ArgumentException("message", nameof(projectCode));
            }

            if (string.IsNullOrWhiteSpace(workItemType))
            {
                throw new ArgumentException("message", nameof(workItemType));
            }

            if (properties is null)
            {
                throw new ArgumentNullException(nameof(properties));
            }

            if (DataProvider is { Write: false })
            {
                throw new InvalidOperationException("DataProvider does not allow write operation");
            }

            WorkItemCreatedResult result;

            await InitAsync();

            if (properties.Count() > 0)
            {
                var newIdentifer = (await DataProvider.NextNumberAsync(projectCode)).ToString();

                var wi = new WorkItem(projectCode, newIdentifer, workItemType, new List<Property>(properties), Array.Empty<LogEntry>());

                // property changes for all values not identical with an empty template.
                var propertyChanges = properties.Where(p => p.Value != EmptyValue).Select(p => new PropertyChange(p.Name, EmptyValue, p.Value));

                var validationResult = await ValidationManager.ValidateAsync(wi, propertyChanges);

                if (validationResult.Count() == 0)
                {
                    await DataProvider.SaveNewWorkItemAsync(wi);

                    result = new WorkItemCreatedResult(true, wi, Array.Empty<ErrorMessage>());
                }
                else
                {
                    result = new WorkItemCreatedResult(false, wi, validationResult);
                }

            }
            else
            {
                result = new WorkItemCreatedResult(false, null, Array.Empty<ErrorMessage>());
            }

            return result;
        }

        public async Task<WorkItem?> GetAsync(string projectCode, string id)
        {
            if (string.IsNullOrWhiteSpace(projectCode))
            {
                throw new ArgumentException("message", nameof(projectCode));
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("message", nameof(id));
            }

            if (DataProvider is { Read: false })
            {
                throw new InvalidOperationException("DataProvider does not allow read operations");
            }

            await InitAsync();

            var workItem = await DataProvider.GetAsync(projectCode, id);

            return workItem;
        }

        public async Task<WorkItemUpdatedResult> UpdateAsync(string projectCode, string id, IEnumerable<Property> properties)
        {
            if (string.IsNullOrWhiteSpace(projectCode))
            {
                throw new ArgumentException("message", nameof(projectCode));
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("message", nameof(id));
            }

            if (DataProvider is { Write: false })
            {
                throw new InvalidOperationException("DataProvider does not allow write operations");
            }

            WorkItemUpdatedResult result;

            await InitAsync();

            var workItem = await GetAsync(projectCode, id);

            if (workItem != null)
            {
                var changes = new List<PropertyChange>();

                var newProperties = workItem.Properties
                    .Select(p =>
                    {
                        var changedValue = properties.FirstOrDefault(changedProperty => changedProperty.Name == p.Name && changedProperty.Value != p.Value);

                        if (changedValue is null)
                        {
                            return p;
                        }
                        else
                        {
                            changes.Add(new PropertyChange(p.Name, p.Value, changedValue.Value));

                            return new Property(p.Name, p.DataType, changedValue.Value);
                        }
                    })
                    .ToArray();

                var newLog = workItem.Log.Union(new LogEntry[] { new LogEntry(DateTimeOffset.Now, "ABC", "Comment", changes) }).ToList();

                workItem = new WorkItem(workItem.ProjectCode, workItem.Id, workItem.WorkItemType, newProperties, newLog);

                var errors = await ValidationManager.ValidateAsync(workItem, changes);

                if (errors.Count() == 0)
                {
                    await DataProvider.SaveUpdatedWorkItemAsync(workItem);

                    result = new WorkItemUpdatedResult(true, workItem, Array.Empty<ErrorMessage>());
                }
                else
                {
                    result = new WorkItemUpdatedResult(false, workItem, errors);
                }
            }
            else
            {
                result = new WorkItemUpdatedResult(false, null, new ErrorMessage[] {
                    new ErrorMessage(nameof(WorkItemManager), string.Empty, $"The work item with id '{id}' in project '{projectCode}' cannot be found.", projectCode, string.Empty, string.Empty),
                });
            }

            return result;
        }
    }
}