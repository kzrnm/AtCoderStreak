using AtCoderStreak.Service;
using ConsoleAppFramework;
using Moq;
using System;

namespace AtCoderStreak.TestUtil
{
    class ProgramBuilder
    {
        const string cookie = "language=ja; REVEL_SESSION=xxxxxxxxxxxx-%00a%3Afalse%00%00UserName%3Ausernam%00%00csrf_token%3Azzzzzzxxxxxxxxxyyyyyyyyy%3D%00%00_TS%3A1604584674%00%00SessionKey%3Axxxxxxxxxxxxxxxxx%00%00Rating%3A1528%00%00UserScreenName%3Ausernam%00%00w%3Afalse%00; _kick_id=2012-04-25+04%3A51; REVEL_FLASH=; timeDelta=-1953";

        public Mock<IDataService> DataMock { set; get; } = new Mock<IDataService>();
        public Mock<IStreakService> StreakMock { set; get; } = new Mock<IStreakService>();
        public Logger Logger { get; } = new Logger();
        public void SetupCookie() => DataMock.Setup(d => d.GetSession()).Returns(cookie);

        public Program Build()
        {
            return new Program(DataMock.Object, StreakMock.Object)
            {
                Context = new ConsoleAppContext(
                      Array.Empty<string>(),
                      DateTime.Now,
                      default,
                      Logger,
                      null,
                      null
                  ),
            };
        }

    }
}
