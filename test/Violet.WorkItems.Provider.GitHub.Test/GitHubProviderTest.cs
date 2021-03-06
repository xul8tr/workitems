using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Violet.WorkItems.Provider.GitHub
{
    public class GitHubProviderTest
    {
        [Fact]
        public async Task GitHubProvider_AllWorkItemsAsync_PublicGitHubQuery()
        {
            // arrange
            var provider = new GitHubDataProvider("violet-workitems-github-unittest", null);

            // act
            var workItems = await provider.ListWorkItemsAsync("violetgrass/projects");

            // assert
            Assert.True(workItems.Count() > 0);
            Assert.True(workItems.All(wi => wi.ProjectCode == "violetgrass/projects"));
            Assert.True(workItems.All(wi => wi.WorkItemType == "Issue"));

            var workItem16 = workItems.FirstOrDefault(wi => wi.Id == "16");
            Assert.NotNull(workItem16);
            Assert.Equal("Migrate to ASP.NET Core 2.0", workItem16[GitHubDataProvider.TitleProperty].Value);
            Assert.Equal("", workItem16[GitHubDataProvider.BodyProperty].Value);
            Assert.Equal("closed", workItem16[GitHubDataProvider.StateProperty].Value);
        }
    }
}
