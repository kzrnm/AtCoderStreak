using AngleSharp.Css;
using AtCoderStreak.Model;
using System.Text.Json.Serialization;
using Xunit.Sdk;

namespace AtCoderStreak;

public record struct SerializableSavedSource(int Id, string TaskUrl, string LanguageId, int Priority, string SourceCode) : IXunitSerializable
{
    void IXunitSerializable.Deserialize(IXunitSerializationInfo info)
    {
        Id = info.GetValue<int>(nameof(Id));
        TaskUrl = info.GetValue<string>(nameof(TaskUrl));
        LanguageId = info.GetValue<string>(nameof(LanguageId));
        Priority = info.GetValue<int>(nameof(Priority));
        SourceCode = info.GetValue<string>(nameof(SourceCode));
    }

    readonly void IXunitSerializable.Serialize(IXunitSerializationInfo info)
    {
        info.AddValue(nameof(Id), Id);
        info.AddValue(nameof(TaskUrl), TaskUrl);
        info.AddValue(nameof(LanguageId), LanguageId);
        info.AddValue(nameof(Priority), Priority);
        info.AddValue(nameof(SourceCode), SourceCode);
    }

    public readonly SavedSource ToSavedSource() => new(Id, TaskUrl, LanguageId, Priority, SourceCode);
}

public class SerializableSubmissionStatus : SubmissionStatus, IXunitSerializable
{
    void IXunitSerializable.Deserialize(IXunitSerializationInfo info)
    {
        Html = info.GetValue<string>(nameof(Html));
        Interval = info.GetValue<long?>(nameof(Interval));
        IsSuccess = info.GetValue<bool>(nameof(IsSuccess));
    }

    void IXunitSerializable.Serialize(IXunitSerializationInfo info)
    {
        info.AddValue(nameof(Html), Html);
        info.AddValue(nameof(Interval), Interval);
        info.AddValue(nameof(IsSuccess), IsSuccess);
    }
}