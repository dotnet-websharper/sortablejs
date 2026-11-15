namespace WebSharper.SortableJs.Sample

open System
open WebSharper
open WebSharper.JavaScript
open WebSharper.UI
open WebSharper.UI.Client
open WebSharper.UI.Templating
open WebSharper.SortableJs

[<JavaScript>]
module Client =

    // The template is the whole document, loaded from the DOM.
    type IndexTemplate = Template<"index.html", ClientLoad.FromDocument>

    // -------------------------------------------------------------------
    // Page state
    // -------------------------------------------------------------------

    let themeStorageKey = "websharper-sortablejs-theme"
    let orderStorageKey = "websharper-sortablejs-order"

    let theme = Var.Create "light"
    let themeLabel = Var.Create "🌙 Dark mode"

    let status = Var.Create "Ready — drag an item to begin."
    let logText = Var.Create ""
    let info = Var.Create "-"
    let version = Var.Create "-"

    /// Every live `Sortable` instance, keyed by the id of its list.
    let mutable instances : (string * Sortable) list = []

    let mutable pluginsMounted = false
    let mutable clickHandler : Action<Dom.Event> option = None

    // -------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------

    let appendLog (line: string) =
        let stamp = DateTime.Now.ToString("HH:mm:ss")
        let entry = stamp + "  " + line
        logText.Value <-
            if logText.Value = "" then entry
            else logText.Value + "\n" + entry

    let setStatus (line: string) =
        status.Value <- line
        appendLog line

    let element (id: string) : HTMLElement option =
        match JS.Document.GetElementById id with
        | null -> None
        | el -> Some (As<HTMLElement> el)

    let addInstance (id: string) (s: Sortable) =
        instances <- (id, s) :: instances

    let instance (id: string) : Sortable option =
        instances
        |> List.tryFind (fun (n, _) -> n = id)
        |> Option.map snd

    let hasInstance (id: string) =
        instances |> List.exists (fun (n, _) -> n = id)

    let destroyInstance (id: string) =
        match instance id with
        | Some s ->
            s.Destroy()
            instances <- instances |> List.filter (fun (n, _) -> n <> id)
            true
        | None -> false

    let applyTheme () =
        JS.Document.DocumentElement.SetAttribute("data-theme", theme.Value)
        themeLabel.Value <-
            if theme.Value = "dark" then "☀️ Light mode" else "🌙 Dark mode"

    let toggleTheme () =
        theme.Value <- (if theme.Value = "dark" then "light" else "dark")
        try JS.Window.LocalStorage.SetItem(themeStorageKey, theme.Value) with _ -> ()
        applyTheme ()

    // -------------------------------------------------------------------
    // Event description
    // -------------------------------------------------------------------

    let itemLabel (el: HTMLElement) =
        if isNull (box el) then "?" else el.TextContent.Trim()

    let listLabel (el: HTMLElement) =
        if isNull (box el) then "?" else "#" + el.Id

    /// SortableJS only fills the multi-drag indices in once the drag starts,
    /// so an `Optional<int>` can also hold `null` at runtime.
    [<Inline "($index === undefined || $index === null) ? \"?\" : \"\" + $index">]
    let indexText (index: int) = X<string>

    let indexLabel (o: Optional<int>) =
        if Optional.isDefined o then indexText o.Value else "?"

    /// The `setData` option receives the native `DataTransfer` of the drag
    /// event; `WebSharper.JavaScript` has no binding for it, so the payload is
    /// set with a one-line inline.
    [<Inline "$dataTransfer.setData($kind, $value)">]
    let setDragData (dataTransfer: obj) (kind: string) (value: string) = X<unit>

    let describe (name: string) (evt: SortableEvent) =
        let items =
            if isNull (box evt.Items) || evt.Items.Length = 0 then ""
            else " items=" + string evt.Items.Length
        let clones =
            if isNull (box evt.Clones) || evt.Clones.Length = 0 then ""
            else " clones=" + string evt.Clones.Length
        let indices =
            if isNull (box evt.OldIndicies) || evt.OldIndicies.Length = 0 then ""
            else
                " oldIndicies=["
                + (evt.OldIndicies
                   |> Array.map (fun i ->
                       indexLabel i.Index + ":" + itemLabel i.MultiDragElement)
                   |> String.concat ",")
                + "] newIndicies=["
                + (if isNull (box evt.NewIndicies) then ""
                   else evt.NewIndicies |> Array.map (fun i -> indexLabel i.Index) |> String.concat ",")
                + "]"
        let swap =
            if isNull (box evt.SwapItem) then ""
            else " swapItem=\"" + itemLabel evt.SwapItem + "\""
        let original =
            if isNull (box evt.OriginalEvent) then ""
            else " originalEvent=" + evt.OriginalEvent.Type
        name + " \"" + itemLabel evt.Item + "\" "
        + listLabel evt.From + "[" + indexLabel evt.OldIndex + "] → "
        + listLabel evt.To + "[" + indexLabel evt.NewIndex + "]"
        + items + clones + indices + swap + original

    let onEvent (name: string) (evt: SortableEvent) =
        appendLog (describe name evt)

    let onMoveEvent (evt: MoveEvent) (_originalEvent: Dom.Event) : obj =
        appendLog (
            "onMove dragged=\"" + itemLabel evt.Dragged + "\" "
            + listLabel evt.From + " → " + listLabel evt.To
            + " related=\"" + itemLabel evt.Related + "\""
        )
        null

    let onFilterEvent (evt: SortableEvent) =
        appendLog ("onFilter \"" + itemLabel evt.Item + "\" (PreventOnFilter = false)")

    // -------------------------------------------------------------------
    // Options
    // -------------------------------------------------------------------

    let storeConfig () =
        StoreConfig(
            Get = (fun (s: Sortable) -> s.ToArray()),
            Set = (fun (s: Sortable) -> setStatus ("store.set ← " + JSON.Stringify(s.ToArray())))
        )

    let boardOptions (listName: string) =
        Options(
            Group = GroupOptions("board", Pull = true, Put = true, RevertClone = true),
            Animation = 180,
            Easing = "cubic-bezier(0.2, 0, 0.2, 1)",
            GhostClass = "sortable-ghost",
            ChosenClass = "sortable-chosen",
            DragClass = "sortable-drag",
            DataIdAttr = "data-id",
            Draggable = "li",
            Handle = ".grip",
            Filter = ".pinned",
            PreventOnFilter = false,
            Ignore = "a, img",
            EmptyInsertThreshold = 12,
            SwapThreshold = 0.65,
            InvertSwap = false,
            Sort = true,
            RemoveCloneOnHide = true,
            Delay = 0,
            DelayOnTouchOnly = false,
            TouchStartThreshold = 2,
            FallbackOnBody = true,
            FallbackClass = "sortable-fallback",
            ForceFallback = false,
            SupportPointer = true,
            Store = storeConfig (),
            SetData = (fun (dataTransfer: obj) (dragEl: HTMLElement) ->
                appendLog ("setData \"" + itemLabel dragEl + "\" (" + listName + ")")
                setDragData dataTransfer "Text" dragEl.TextContent),
            OnStart = onEvent "onStart",
            OnChoose = onEvent "onChoose",
            OnUnchoose = onEvent "onUnchoose",
            OnSort = onEvent "onSort",
            OnUpdate = onEvent "onUpdate",
            OnAdd = onEvent "onAdd",
            OnRemove = onEvent "onRemove",
            OnChange = onEvent "onChange",
            OnEnd = onEvent "onEnd",
            OnClone = onEvent "onClone",
            OnFilter = onFilterEvent,
            OnMove = onMoveEvent
        )

    let multiOptions () =
        Options(
            Group = "plugins",
            Animation = 150,
            GhostClass = "sortable-ghost",
            ChosenClass = "sortable-chosen",
            DragClass = "sortable-drag",
            DataIdAttr = "data-id",
            MultiDrag = true,
            SelectedClass = "multi-selected",
            MultiDragKey = "CTRL",
            AvoidImplicitDeselect = false,
            OnSelect = onEvent "onSelect",
            OnDeselect = onEvent "onDeselect",
            OnStart = onEvent "onStart",
            OnEnd = onEvent "onEnd",
            OnSort = onEvent "onSort",
            OnUpdate = onEvent "onUpdate"
        )

    let swapOptions () =
        Options(
            Animation = 150,
            DataIdAttr = "data-id",
            Swap = true,
            SwapClass = "swap-highlight",
            OnStart = onEvent "onStart",
            OnEnd = onEvent "onEnd",
            OnSort = onEvent "onSort",
            OnUpdate = onEvent "onUpdate"
        )

    let spillOptions (revert: bool) =
        let o =
            Options(
                Animation = 150,
                DataIdAttr = "data-id",
                GhostClass = "sortable-ghost",
                OnSpill = onEvent "onSpill",
                OnEnd = onEvent "onEnd"
            )
        if revert then o.RevertOnSpill <- true else o.RemoveOnSpill <- true
        o

    let scrollOptions () =
        Options(
            Animation = 120,
            DataIdAttr = "data-id",
            Scroll = true,
            ScrollSensitivity = 60,
            ScrollSpeed = 12,
            BubbleScroll = true,
            ForceAutoScrollFallback = true,
            OnEnd = onEvent "onEnd"
        )

    // -------------------------------------------------------------------
    // Lists
    // -------------------------------------------------------------------

    let createList (id: string) (options: Options) =
        match element id with
        | Some el ->
            if hasInstance id then
                setStatus ("#" + id + " already has a Sortable instance.")
            else
                let s = Sortable.Create(el, options)
                addInstance id s
                appendLog ("Sortable.create(#" + id + ")")
        | None -> setStatus ("#" + id + " is missing from the page.")

    let createBoard () =
        createList "todo" (boardOptions "todo")
        createList "doing" (boardOptions "doing")
        createList "done" (boardOptions "done")
        setStatus "Board ready — three lists share the group \"board\"."

    let destroyBoard () =
        let destroyed =
            [ "todo"; "doing"; "done" ] |> List.filter destroyInstance
        setStatus (
            if destroyed.IsEmpty then "The board has no instances to destroy."
            else "destroy() on " + String.concat ", " destroyed + "."
        )

    /// MultiDrag and Swap are the only plugins that need mounting: the
    /// `sortablejs` root entry point already mounts AutoScroll and OnSpill.
    let mountPlugins () =
        if not pluginsMounted then
            Sortable.Mount(MultiDrag(), Swap())
            pluginsMounted <- true
            setStatus "Sortable.mount(MultiDrag(), Swap()) — plugins mounted."

    let createPluginLists () =
        mountPlugins ()
        createList "multi" (multiOptions ())
        createList "swap" (swapOptions ())
        setStatus "MultiDrag and Swap lists ready — Ctrl-click to select several items."

    let destroyPluginLists () =
        let destroyed = [ "multi"; "swap" ] |> List.filter destroyInstance
        setStatus (
            if destroyed.IsEmpty then "The plugin lists are not created."
            else "destroy() on " + String.concat ", " destroyed + "."
        )

    let createSpillLists () =
        createList "spill-revert" (spillOptions true)
        createList "spill-remove" (spillOptions false)
        setStatus "Spill lists ready — drop an item outside a list."

    let destroySpillLists () =
        let destroyed = [ "spill-revert"; "spill-remove" ] |> List.filter destroyInstance
        setStatus (
            if destroyed.IsEmpty then "The spill lists are not created."
            else "destroy() on " + String.concat ", " destroyed + "."
        )

    let resetSpillList (id: string) (items: string list) =
        match element id with
        | Some el ->
            el.InnerHTML <- ""
            for item in items do
                let li = JS.Document.CreateElement("li")
                li.SetAttribute("data-id", item)
                li.TextContent <- "Item " + item
                el.AppendChild(li) |> ignore
        | None -> ()

    let resetSpillLists () =
        resetSpillList "spill-remove" [ "rm1"; "rm2"; "rm3" ]
        resetSpillList "spill-revert" [ "rv1"; "rv2"; "rv3" ]
        setStatus "Spill lists refilled."

    let createScrollList () =
        createList "scroll" (scrollOptions ())
        setStatus "Scroll list ready — drag an item to the top or bottom edge."

    let destroyScrollList () =
        if destroyInstance "scroll" then setStatus "destroy() on #scroll."
        else setStatus "The scroll list is not created."

    // -------------------------------------------------------------------
    // Order persistence
    // -------------------------------------------------------------------

    let saveOrder () =
        let data =
            [ "todo"; "doing"; "done" ]
            |> List.choose (fun id ->
                instance id
                |> Option.map (fun s -> [| id; JSON.Stringify(s.ToArray()) |]))
            |> Array.ofList
        let json = JSON.Stringify(data)
        JS.Window.LocalStorage.SetItem(orderStorageKey, json)
        info.Value <- json
        setStatus "Saved the data-id order of the board to localStorage."

    let restoreOrder () =
        let raw = try JS.Window.LocalStorage.GetItem(orderStorageKey) with _ -> null
        if isNull raw then setStatus "No saved order yet — press \"Save order\" first."
        else
            let data = JSON.Parse(raw) |> As<string[][]>
            for row in data do
                if row.Length = 2 then
                    match instance row.[0] with
                    | Some s ->
                        let ids = JSON.Parse(row.[1]) |> As<string[]>
                        s.Sort(ids, true)
                    | None -> ()
            setStatus "Restored the saved order with sort(order, useAnimation = true)."

    // -------------------------------------------------------------------
    // Instance API
    // -------------------------------------------------------------------

    let toArray () =
        match instance "todo" with
        | Some s ->
            let ids = s.ToArray()
            info.Value <-
                "toArray() = [" + String.concat ", " ids + "]"
                + "\ndataIdAttr = " + s.Options.DataIdAttr
            setStatus "toArray() reads the data-id attributes of #todo."
        | None -> setStatus "Create the board first."

    let sortReversed () =
        match instance "todo" with
        | Some s ->
            let reversed = Array.rev (s.ToArray())
            s.Sort(reversed, true)
            info.Value <- "sort([" + String.concat ", " reversed + "], true)"
            setStatus "sort() reversed the first list."
        | None -> setStatus "Create the board first."

    let sortById () =
        match instance "doing" with
        | Some s ->
            let sorted = Array.sort (s.ToArray())
            s.Sort(sorted, false)
            info.Value <- "sort([" + String.concat ", " sorted + "])"
            setStatus "sort() sorted the second list by data-id (no animation)."
        | None -> setStatus "Create the board first."

    let saveStore () =
        match instance "todo" with
        | Some s ->
            s.Save()
            info.Value <- "save() → the store.set callback ran"
            setStatus "save() calls the store.set callback of the instance."
        | None -> setStatus "Create the board first."

    let optionGetSet () =
        match instance "todo" with
        | Some s ->
            let before = s.Option("animation")
            s.Option("animation", box 400)
            let after = s.Option("animation")
            info.Value <-
                "option(\"animation\"): " + string before + " → " + string after
                + "\noption(\"group\") = " + string (s.Option("group"))
            setStatus "option(name) / option(name, value) read and wrote the animation."
            s.Option("animation", box 180)
        | None -> setStatus "Create the board first."

    let inspect () =
        let describeEl (x: HTMLElement) =
            if isNull (box x) then "null" else "<" + x.TagName.ToLower() + "> " + itemLabel x
        let describeInst (x: Sortable) =
            if isNull (box x) then "null" else "a Sortable instance"
        info.Value <-
            "Sortable.version = " + Sortable.Version
            + "\nSortable.active = " + describeInst Sortable.Active
            + "\nSortable.dragged = " + describeEl Sortable.Dragged
            + "\nSortable.ghost = " + describeEl Sortable.Ghost
            + "\nSortable.clone = " + describeEl Sortable.Clone
            + "\nSortable.get(#todo) = "
            + (match element "todo" with
               | Some el -> describeInst (Sortable.Get el)
               | None -> "#todo missing")
        setStatus "Read the static properties and Sortable.get(el)."

    let closestFromItem () =
        match element "todo", instance "todo" with
        | Some el, Some s ->
            let child = el.Children.Item(0) |> As<HTMLElement>
            let found = s.Closest(child, "ul")
            info.Value <-
                "closest(li, \"ul\") = " + (if isNull (box found) then "null" else "#" + found.Id)
            setStatus "closest() walks up to the matching ancestor."
        | _ -> setStatus "Create the board first."

    let newInstance () =
        match element "done" with
        | Some el when not (hasInstance "done") ->
            let s = new Sortable(el, Options(Animation = 120, Sort = true))
            addInstance "done" s
            info.Value <- "new Sortable(#done, Options(Animation = 120, Sort = true))"
            setStatus "Constructed a Sortable instance directly."
        | Some el ->
            info.Value <- "Sortable.get(#done) already exists — destroying it first."
            destroyInstance "done" |> ignore
            let s = Sortable.Create(el, boardOptions "done")
            addInstance "done" s
            setStatus "Re-created the third list with Sortable.create()."
        | None -> setStatus "#done is missing from the page."

    // -------------------------------------------------------------------
    // Sortable.utils
    // -------------------------------------------------------------------

    let utils () = Sortable.Utils

    let utilsFind () =
        match element "todo" with
        | Some el ->
            let items = utils().Find(el, "li")
            info.Value <- "find(#todo, \"li\").length = " + string items.Length
            setStatus "utils.find() collected the list items."
        | None -> setStatus "#todo is missing."

    let utilsIndex () =
        match element "todo" with
        | Some el ->
            let child = el.Children.Item(1) |> As<HTMLElement>
            info.Value <- "index(second item) = " + string (utils().Index(child))
            setStatus "utils.index() reports the position of an item."
        | None -> setStatus "#todo is missing."

    let utilsIs () =
        match element "todo" with
        | Some el ->
            let child = el.Children.Item(2) |> As<HTMLElement>
            info.Value <-
                "is(item, \".pinned\") = " + string (utils().Is(child, ".pinned"))
            setStatus "utils.is() matches a selector."
        | None -> setStatus "#todo is missing."

    let utilsToggleClass () =
        match element "todo" with
        | Some el ->
            let child = el.Children.Item(0) |> As<HTMLElement>
            let on = not (child.ClassList.Contains("toggled"))
            utils().ToggleClass(child, "toggled", on)
            info.Value <- "toggleClass(first item, \"toggled\", " + string on + ")"
            setStatus "utils.toggleClass() added or removed the class."
        | None -> setStatus "#todo is missing."

    let utilsClosest () =
        match element "todo" with
        | Some el ->
            let child = el.Children.Item(0) |> As<HTMLElement>
            let found = utils().Closest(child, "ul", el, true)
            info.Value <-
                "closest(item, \"ul\", #todo, true) = "
                + (if isNull (box found) then "null" else "#" + found.Id)
            setStatus "utils.closest() found the enclosing list."
        | None -> setStatus "#todo is missing."

    let utilsCss () =
        match element "todo" with
        | Some el ->
            let style = utils().Css(el)
            utils().Css(el, "outline", "2px solid #4f46e5")
            let radius = style.GetPropertyValue("border-radius")
            info.Value <-
                "css(el).getPropertyValue(\"border-radius\") = " + radius
                + " · css(el, \"outline\", …) set"
            setStatus "utils.css() read the style object and wrote an inline style."
        | None -> setStatus "#todo is missing."

    let utilsGetChild () =
        match element "todo", instance "todo" with
        | Some el, Some s ->
            let child = utils().GetChild(el, 1, s.Options, false)
            info.Value <-
                "getChild(#todo, 1) = " + (if isNull (box child) then "null" else itemLabel child)
            setStatus "utils.getChild() counted the draggable children."
        | _ -> setStatus "Create the board first."

    let utilsDirection () =
        match element "todo", instance "todo" with
        | Some el, Some s ->
            let direction = utils().DetectDirection(el, s.Options)
            info.Value <- "detectDirection(#todo) = " + As<string> (box direction)
            setStatus "utils.detectDirection() inspected the flex direction."
        | _ -> setStatus "Create the board first."

    let utilsNextTick () =
        let id = utils().NextTick(Action(fun () -> appendLog "nextTick callback ran"))
        info.Value <- "nextTick() returned id " + string id
        setStatus "utils.nextTick() scheduled a callback."

    let utilsCancelNextTick () =
        let id = utils().NextTick(Action(fun () -> appendLog "this should not run"))
        utils().CancelNextTick(id)
        info.Value <- "cancelNextTick(" + string id + ")"
        setStatus "utils.cancelNextTick() cancelled the pending callback."

    let utilsClone () =
        match element "todo" with
        | Some el ->
            let child = el.Children.Item(0) |> As<HTMLElement>
            let copy = utils().Clone(child)
            copy.ClassList.Add("toggled")
            info.Value <- "clone(first item) = \"" + itemLabel copy + "\" (not attached)"
            setStatus "utils.clone() copied the element node."
        | None -> setStatus "#todo is missing."

    let utilsExtend () =
        let merged = utils().Extend(box (FallbackOffset(X = 1, Y = 2)), box (FallbackOffset(Y = 42)))
        info.Value <- "extend({x:1,y:2}, {y:42}) = " + JSON.Stringify(merged)
        setStatus "utils.extend() merged two option objects."

    let utilsThrottle () =
        let throttled = utils().Throttle(Action(fun () -> appendLog "throttle tick"), 250)
        let call = As<unit -> unit> throttled
        call ()
        info.Value <- "throttle(callback, 250) returned a function; called it once"
        setStatus "utils.throttle() wrapped the callback."

    let utilsExpando () =
        info.Value <- "expando = \"" + utils().Expando + "\""
        setStatus "utils.expando is the property SortableJS stores instances on."

    let utilsOn () =
        match element "todo" with
        | Some el ->
            let handler = Action<Dom.Event>(fun _ -> setStatus "utils.on: click on #todo")
            utils().On(el, "click", handler)
            clickHandler <- Some handler
            setStatus "utils.on() attached a click listener."
        | None -> setStatus "#todo is missing."

    let utilsOff () =
        match element "todo", clickHandler with
        | Some el, Some handler ->
            utils().Off(el, "click", handler)
            clickHandler <- None
            setStatus "utils.off() removed the click listener."
        | _ -> setStatus "There is no listener to remove."

    let selectAll () =
        match element "multi" with
        | Some el ->
            for i in 0 .. 1 do
                el.Children.Item(i) |> As<HTMLElement> |> utils().Select
            setStatus "utils.select() selected the first two items."
        | None -> setStatus "#multi is missing."

    let deselectAll () =
        match element "multi" with
        | Some el ->
            for i in 0 .. 1 do
                el.Children.Item(i) |> As<HTMLElement> |> utils().Deselect
            setStatus "utils.deselect() cleared the selection."
        | None -> setStatus "#multi is missing."

    // -------------------------------------------------------------------
    // Entry point
    // -------------------------------------------------------------------

    [<SPAEntryPoint>]
    let Main () =
        // Restore the stored theme before the first render.
        let stored = try JS.Window.LocalStorage.GetItem(themeStorageKey) with _ -> null
        if stored = "dark" || stored = "light" then theme.Value <- stored
        applyTheme ()

        version.Value <- Sortable.Version

        IndexTemplate()
            .status(status.V)
            .logText(logText.V)
            .info(info.V)
            .version(version.V)
            .themeLabel(themeLabel.V)
            .ToggleTheme(fun _ -> toggleTheme ())
            .CreateBoard(fun _ -> createBoard ())
            .DestroyBoard(fun _ -> destroyBoard ())
            .SaveOrder(fun _ -> saveOrder ())
            .RestoreOrder(fun _ -> restoreOrder ())
            .MountPlugins(fun _ -> createPluginLists ())
            .DestroyPlugins(fun _ -> destroyPluginLists ())
            .SelectAll(fun _ -> selectAll ())
            .DeselectAll(fun _ -> deselectAll ())
            .CreateSpillLists(fun _ -> createSpillLists ())
            .DestroySpillLists(fun _ -> destroySpillLists ())
            .ResetSpillLists(fun _ -> resetSpillLists ())
            .CreateScrollList(fun _ -> createScrollList ())
            .DestroyScrollList(fun _ -> destroyScrollList ())
            .ToArray(fun _ -> toArray ())
            .SortReversed(fun _ -> sortReversed ())
            .SortById(fun _ -> sortById ())
            .SaveStore(fun _ -> saveStore ())
            .OptionGetSet(fun _ -> optionGetSet ())
            .Inspect(fun _ -> inspect ())
            .ClosestFromItem(fun _ -> closestFromItem ())
            .NewInstance(fun _ -> newInstance ())
            .UtilsFind(fun _ -> utilsFind ())
            .UtilsIndex(fun _ -> utilsIndex ())
            .UtilsIs(fun _ -> utilsIs ())
            .UtilsToggleClass(fun _ -> utilsToggleClass ())
            .UtilsClosest(fun _ -> utilsClosest ())
            .UtilsCss(fun _ -> utilsCss ())
            .UtilsGetChild(fun _ -> utilsGetChild ())
            .UtilsDirection(fun _ -> utilsDirection ())
            .UtilsNextTick(fun _ -> utilsNextTick ())
            .UtilsCancelNextTick(fun _ -> utilsCancelNextTick ())
            .UtilsClone(fun _ -> utilsClone ())
            .UtilsExtend(fun _ -> utilsExtend ())
            .UtilsThrottle(fun _ -> utilsThrottle ())
            .UtilsExpando(fun _ -> utilsExpando ())
            .UtilsOn(fun _ -> utilsOn ())
            .UtilsOff(fun _ -> utilsOff ())
            .Bind()

        // The template is bound: all the `#id` elements above exist now.
        // Plugins must be mounted *before* the instances that use them.
        mountPlugins ()
        createBoard ()
        createPluginLists ()
        createSpillLists ()
        createScrollList ()
        restoreOrder ()