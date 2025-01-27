using System.Threading.Tasks;
using Xunit;

namespace AtCoderStreak
{
    public class ProgramTests
    {
        [Fact]
        public async Task TestAdd_Failed()
        {
            Program.GetDefault().ShouldNotBeNull();
        }
    }
}
