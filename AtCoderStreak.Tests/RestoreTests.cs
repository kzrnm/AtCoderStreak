using AtCoderStreak.Model;
using AtCoderStreak.TestUtil;
using FluentAssertions;
using Moq;
using System;
using Xunit;

namespace AtCoderStreak
{
    public class RestoreTests
    {
        readonly ProgramBuilder pb = new();

        [Fact]
        public async void TestRestore_NoArgs()
        {
            pb.Build()
                .Invoking(p => p.RestoreInternal())
                .Should()
                .ThrowExactly<ArgumentException>()
                .WithMessage("Error: must use either url or id");
        }

        [Fact]
        public async void TestRestore_BothArgs()
        {
            pb.Build()
                .Invoking(p => p.RestoreInternal(10, "example.com/contests/ex3/tasks/ex3_2"))
                .Should()
                .ThrowExactly<ArgumentException>()
                .WithMessage("Error: must use either url or id");
        }


        [Fact]
        public async void TestRestore_Id()
        {
            pb.DataMock
                .Setup(d => d.GetSourceById(1))
                .Returns(new SavedSource(1, "example.com/contests/ex3/tasks/ex3_2", "4000", 10, "1\n2"));

            var p = pb.Build();
            var ret = p.RestoreInternal(id: 1);
            ret.Should().Be(new SavedSource(1, "example.com/contests/ex3/tasks/ex3_2", "4000", 10, "1\n2"));

            pb.DataMock.Verify(d => d.GetSourceById(1), Times.Once());
            pb.DataMock.Verify(d => d.GetSourceById(It.IsAny<int>()), Times.Once());
        }

        [Fact]
        public async void TestRestore_Url()
        {
            pb.DataMock
                .Setup(d => d.GetSourcesByUrl("example.com/contests/ex3/tasks/ex3_2"))
                .Returns(new[] { new SavedSource(1, "example.com/contests/ex3/tasks/ex3_2", "4000", 10, "1\n2") });

            var p = pb.Build();
            var ret = p.RestoreInternal(url: "example.com/contests/ex3/tasks/ex3_2");
            ret.Should().Be(new SavedSource(1, "example.com/contests/ex3/tasks/ex3_2", "4000", 10, "1\n2"));

            pb.DataMock.Verify(d => d.GetSourcesByUrl("example.com/contests/ex3/tasks/ex3_2"), Times.Once());
            pb.DataMock.Verify(d => d.GetSourcesByUrl(It.IsAny<string>()), Times.Once());
        }
    }
}
