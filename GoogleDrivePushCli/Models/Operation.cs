using System;

namespace GoogleDrivePushCli.Models;

public class Operation
{
    public string Description { get; set; }
    public Action Action { get; set; }
}