using System;
using System.Threading.Tasks;
using Violet.WorkItems.Provider;
using Violet.WorkItems.Types;
using Violet.WorkItems.Types.CommonSdlc;
using Xunit;

namespace Violet.WorkItems.Validation
{
    public class CompletenessValidatorTest
    {
        [Fact]
        public async Task CompletenessValidator_Validate_Success()
        {
            // arrange
            WorkItemManager manager = BuildManager();

            var properties = new Property[] {
                new Property("A", "String", "aa"),
                new Property("B", "String", string.Empty), // as the template would create it
            };

            // act
            var result = await manager.CreateAsync("FOO", "BAR", properties);

            // assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.CreatedWorkItem);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task CompletenessValidator_Validate_Error()
        {
            // arrange
            WorkItemManager manager = BuildManager();

            var properties = new Property[] {
                new Property("B", "String", "bb"),
            };

            // act
            var result = await manager.CreateAsync("FOO", "BAR", properties);

            // assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.NotNull(result.CreatedWorkItem);
            Assert.Collection(result.Errors,
                em =>
                {
                    Assert.Equal(nameof(CompletenessValidator), em.Source);
                    Assert.Equal(string.Empty, em.ErrorCode);
                    Assert.Equal("FOO", em.ProjectCode);
                    Assert.Equal("1", em.Id);
                    Assert.Equal("A", em.Property);
                }
            );
        }

        private static WorkItemManager BuildManager()
        {
            return new WorkItemManager(new InMemoryDataProvider(), new InMemoryDescriptorProvider(
                new WorkItemDescriptor("BAR", new LogDescriptor(true, Array.Empty<LogEntryTypeDescriptor>()), new PropertyDescriptor[] {
                    new PropertyDescriptor("A", "String"),
                    new PropertyDescriptor("B", "String"),
                }, Array.Empty<StageDescriptor>())
            ));
        }
    }
}