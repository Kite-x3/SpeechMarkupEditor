// Copyright (C) Neurosoft

using Avalonia.Controls;
using CommunityToolkit.Mvvm.Messaging;
using SpeechMarkupEditor.Assets;
using SpeechMarkupEditor.Messages;

namespace SpeechMarkupEditor.Views;

public partial class WordMarkerDialog : Window
{
    public WordMarkerDialog()
    {
        InitializeComponent();
        WindowIconFactory.ApplyAppIcon(this);

        WeakReferenceMessenger.Default.Register<WordMarkerDialog, CloseDialogMessage>(
            this, (recipient, message)
                => recipient.Close());

        Closing += (sender, e) =>
        {
            if (!WeakReferenceMessenger.Default.IsRegistered<WordMarkerSubmittedMessage>(this))
            {
                WeakReferenceMessenger.Default.Send(new WordMarkerSubmittedMessage(null));
            }
        };
    }
}
