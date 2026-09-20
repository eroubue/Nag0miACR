using PromeRotation.Data;

namespace Nag0mi.Gunbreaker.Data;

public static class QT
{
    public static bool QTGET(string qtName) => PromeSettings.Instance.GetQt(qtName);

    public static void QTSET(string qtName, bool qtValue) =>
        PromeSettings.Instance.SetQt(qtName, qtValue);
}
