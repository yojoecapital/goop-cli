using System;

namespace GoogleDrivePushCli.Utilities;

public class SynchronousProgress(Action<double> report) : IProgress<double>
{
    public static readonly IProgress<double> None = new SynchronousProgress(_ => { });

    public void Report(double value) => report(value);
}
