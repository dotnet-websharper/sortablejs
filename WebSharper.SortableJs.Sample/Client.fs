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
