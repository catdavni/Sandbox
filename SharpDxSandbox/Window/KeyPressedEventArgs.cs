namespace SharpDxSandbox.Window
{
    public sealed class KeyPressedEventArgs : EventArgs
    {
        public KeyPressedEventArgs(string input) => Input = input;

        public string Input { get; }
    }
}
