using Dock.Model.ReactiveUI.Navigation.Controls;
using ReactiveUI;

namespace PuppyWorkbooks.App.Models.Docking;

public class DocumentViewModel : RoutableDocument
{

    public DocumentViewModel(IScreen host, DocumentEditorViewModel documentToOpen) : base(host)
    {
        Router.Navigate.Execute(documentToOpen);
        
    }

    public void InitNavigation(
        IRoutableViewModel? document)
    {
        // if (document is not null)
        // {
        //     GoDocument = ReactiveCommand.Create(() =>
        //         HostScreen.Router.Navigate.Execute(document).Subscribe(_ => { }));
        // }

    }
}
