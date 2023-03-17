using AtCoderStreak.Model;
using AtCoderStreak.TestUtil;
using FluentAssertions;
using Moq;
using System.Threading.Tasks;
using Xunit;

namespace AtCoderStreak
{
    public class RestoreTests
    {
        readonly MockProgram pb = new();

        [Fact]
        public async Task TestRestore_NoArgs()
        {
            using var file = new TemporaryFile();
            var ret = await pb.RunCommand("restore", "-f", file.Path);
            ret.Should().Be(128);
        }

        [Fact]
        public async Task TestRestore_BothArgs()
        {
            using var file = new TemporaryFile();
            var ret = await pb.RunCommand("restore", "10", "-u", "example.com/contests/ex3/tasks/ex3_2", "-f", file.Path);
            ret.Should().Be(128);
        }


        [Fact]
        public async Task TestRestore_Call_Id()
        {
            pb.DataMock
                .Setup(d => d.GetSourceById(1))
                .Returns(new SavedSource(1, "example.com/contests/ex3/tasks/ex3_2", "4000", 10, "1\n2"));

            using var file = new TemporaryFile();
            var ret = await pb.RunCommand("restore", "1", "-f", file.Path);
            ret.Should().Be(0);
        }

        [Fact]
        public async Task TestRestore_Call_Url()
        {
            pb.DataMock
                .Setup(d => d.GetSourcesByUrl("example.com/contests/ex3/tasks/ex3_2"))
                .Returns(new[] { new SavedSource(1, "example.com/contests/ex3/tasks/ex3_2", "4000", 10, "1\n2") });

            using var file = new TemporaryFile();
            var ret = await pb.RunCommand("restore", "-u", "example.com/contests/ex3/tasks/ex3_2", "-f", file.Path);
            ret.Should().Be(0);
        }

        [Fact]
        public async Task TestRestore_LongName()
        {
            pb.DataMock
                .Setup(d => d.GetSourcesByUrl("example.com/contests/ex3/tasks/ex3_2"))
                .Returns(new[] { new SavedSource(1, "example.com/contests/ex3/tasks/ex3_2", "4000", 10, "1\n2") });

            using var file = new TemporaryFile();
            var ret = await pb.RunCommand("restore", "--url", "example.com/contests/ex3/tasks/ex3_2", "--file", file.Path);
            ret.Should().Be(0);
        }

        [Fact]
        public async Task TestRestore_Id()
        {
            pb.DataMock
                .Setup(d => d.GetSourceById(1))
                .Returns(new SavedSource(1, "example.com/contests/ex3/tasks/ex3_2", "4000", 10, "1\n2"));

            var p = pb;
            var ret = p.RestoreInternal(id: 1);
            ret.Should().Be(new SavedSource(1, "example.com/contests/ex3/tasks/ex3_2", "4000", 10, "1\n2"));

            pb.DataMock.Verify(d => d.GetSourceById(1), Times.Once());
            pb.DataMock.Verify(d => d.GetSourceById(It.IsAny<int>()), Times.Once());
        }

        [Fact]
        public async Task TestRestore_Url()
        {
            pb.DataMock
                .Setup(d => d.GetSourcesByUrl("example.com/contests/ex3/tasks/ex3_2"))
                .Returns(new[] { new SavedSource(1, "example.com/contests/ex3/tasks/ex3_2", "4000", 10, "1\n2") });

            var p = pb;
            var ret = p.RestoreInternal(url: "example.com/contests/ex3/tasks/ex3_2");
            ret.Should().Be(new SavedSource(1, "example.com/contests/ex3/tasks/ex3_2", "4000", 10, "1\n2"));

            pb.DataMock.Verify(d => d.GetSourcesByUrl("example.com/contests/ex3/tasks/ex3_2"), Times.Once());
            pb.DataMock.Verify(d => d.GetSourcesByUrl(It.IsAny<string>()), Times.Once());
        }
    }
}
