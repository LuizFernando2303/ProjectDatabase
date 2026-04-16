using System;

public interface IAction<TEventArgs> where TEventArgs : EventArgs
{
    void Execute(object sender, TEventArgs e);

    EventHandler<TEventArgs> Handler { get; }
}