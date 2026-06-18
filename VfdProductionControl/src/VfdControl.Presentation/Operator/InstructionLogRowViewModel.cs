namespace VfdControl.Presentation.Operator;

public sealed class InstructionLogRowViewModel
{
    public InstructionLogRowViewModel(int number, string command, string result, string send, string receive)
    {
        Number = number;
        Command = command;
        Result = result;
        Send = send;
        Receive = receive;
    }

    public int Number { get; }

    public string Command { get; }

    public string Result { get; }

    public string Send { get; }

    public string Receive { get; }
}
