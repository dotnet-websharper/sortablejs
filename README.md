# WebSharper SortableJS API Binding

This repository provides an F# [WebSharper](https://websharper.com/) binding for [SortableJS](https://sortablejs.github.io/Sortable/), enabling WebSharper applications to create draggable, sortable lists easily.

## Repository Structure

The repository consists of two main projects:

1. **Binding Project**:

   - Contains the F# WebSharper binding for SortableJS.

2. **Sample Project**:
   - Demonstrates how to use SortableJS with WebSharper syntax.

## Installation

To use this package in your WebSharper project, add the NuGet package:

```bash
   dotnet add package WebSharper.SortableJs
```

## Building

### Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) installed on your machine.
- Node.js and npm (for building web assets).

### Steps

1. Clone the repository:

   ```bash
   git clone https://github.com/dotnet-websharper/SortableJs.git
   cd SortableJs
   ```

2. Build the Binding Project:

   ```bash
   dotnet build WebSharper.SortableJs/WebSharper.SortableJs.fsproj
   ```

3. Build and Run the Sample Project:

   ```bash
   cd WebSharper.SortableJs.Sample
   dotnet build
   dotnet run
   ```

4. Open the sample project in your browser to see SortableJS in action.

## Example Usage

Below is an example of how to use SortableJS in a WebSharper project:

```fsharp
namespace WebSharper.SortableJs.Sample

open WebSharper
open WebSharper.JavaScript
open WebSharper.UI
open WebSharper.UI.Client
open WebSharper.UI.Templating
open WebSharper.SortableJs

[<JavaScript>]
module Client =
    // The templates are loaded from the DOM, so you just can edit index.html
    // and refresh your browser, no need to recompile unless you add or remove holes.
    type IndexTemplate = Template<"wwwroot/index.html", ClientLoad.FromDocument>

    let getHtmlElement (id: string) =
        JS.Document.GetElementById(id) |> As<HTMLElement>

    let createSortable (element: HTMLElement) (listName: string) =
        printfn("create sortable")
        Sortable.Create(element, Options(
            Group = "shared",
            Animation = 150,
            OnEnd = (fun evt ->
                printfn($"Moved item on {listName}")
            )
        )) |> ignore

    let saveOrder() =
        let getListItems (list: Dom.Element) =
            let children = list.Children
            [|
                for i in 0 .. children.Length - 1 do
                    let el = children.Item(i) |> As<Dom.Element>
                    yield el.TextContent
            |]

        let order =
            {|
                list1 = getListItems <| getHtmlElement("list1")
                list2 = getListItems <| getHtmlElement("list2")
            |}

        JS.Window.LocalStorage.SetItem("sortableOrder", JSON.Stringify(order))
        JS.Alert("✅ Order saved to localStorage!")

    let populateList (list: Dom.Element) (items: string array) =
        list.InnerHTML <- ""
        for item in items do
            let li = JS.Document.CreateElement("li")
            li.TextContent <- item
            list.AppendChild(li) |> ignore

    let loadOrder () =
        let saved = JSON.Parse(JS.Window.LocalStorage.GetItem("sortableOrder"))
        if not (isNull saved) then
            populateList (getHtmlElement("list1")) (saved?list1 |> As<string[]>)
            populateList (getHtmlElement("list2")) (saved?list2 |> As<string[]>)

    [<SPAEntryPoint>]
    let Main () =
        JS.Window.OnLoad <- fun _ -> loadOrder()

        IndexTemplate.Main()
            .ListContainerInit(fun () ->
                createSortable (getHtmlElement("list1")) "list1"
                createSortable (getHtmlElement("list2")) "list2"
            )
            .SaveOrder(fun _ -> saveOrder())
            .Doc()
        |> Doc.RunById "main"
```

This example demonstrates creating sortable lists, moving items between them, and saving/restoring their order using localStorage.

## Important Considerations

- **Cross-Browser Compatibility**: SortableJS works well across modern browsers.
- **Data Persistence**: Storing order in localStorage ensures that the sorting persists after page reloads.
