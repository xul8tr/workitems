using System;
using Violet.WorkItems.Types;

namespace Violet.WorkItems.Provider
{
    internal class GitHubIssueDescription
    {
        public WorkItemDescriptor Model
            => new WorkItemDescriptor("GitHubIssues",
                new LogDescriptor(false, new LogEntryTypeDescriptor[] {
                    new LogEntryTypeDescriptor("assignmentChange"),
                    new LogEntryTypeDescriptor("milestoneChange"),
                    new LogEntryTypeDescriptor("labelChange"),
                    new LogEntryTypeDescriptor("stateChange"),
                }),
                new PropertyDescriptor[] {
                    new PropertyDescriptor("State", "String", propertyType: PropertyType.SingleValue, isEditable: false, initialValue: "Open", valueProvider: new EnumValueProviderDescriptor(new EnumValue("Open", "Open"), new EnumValue("Closed", "Closed"))),
                    new PropertyDescriptor("Title", "String", validators: new ValidatorDescriptor[] {
                        new StringLengthValidatorDescriptor(3, 1000),
                        new MandatoryValidatorDescriptor(),
                    }),
                    new PropertyDescriptor("Description", "String", validators: new ValidatorDescriptor[] {
                        new StringLengthValidatorDescriptor(0, 4000),
                    }),
                    new PropertyDescriptor("Label", "String", propertyType: PropertyType.MultipleValue, valueProvider: new ProjectCollectionValueProviderDescriptor("labels")),
                    new PropertyDescriptor("Milestone", "String", propertyType: PropertyType.MultipleValue, valueProvider: new ProjectCollectionValueProviderDescriptor("milestones")),
                    new PropertyDescriptor("Assignee", "String", propertyType: PropertyType.MultipleValue, valueProvider: new ProjectUserValueProviderDescriptor(string.Empty)),
                },
                new StageDescriptor[] {
                    new StageDescriptor("stage-Open", new PropertyValueConditionDescriptor("State", "Open"),
                        Array.Empty<StagePropertyDescriptor>(),
                        new CommandDescriptor[] {
                            new ChangePropertyValueCommandDescriptor("Close", "Close", "State", "Closed"),
                        }
                    ),
                }
            );
    }
}