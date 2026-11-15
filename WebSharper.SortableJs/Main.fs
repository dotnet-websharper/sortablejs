namespace WebSharper.SortableJs

open WebSharper
open WebSharper.JavaScript
open WebSharper.InterfaceGenerator
open WebSharper.TouchEvents

#nowarn "3886" // single-element Required/Optional lists are intentional

module Definition =

    // SortableJS 1.15 ships three ES module bundles:
    //
    //   sortablejs                                  -> Sortable, MultiDrag, Swap
    //                                                  (AutoScroll and OnSpill mounted)
    //   sortablejs/modular/sortable.core.esm.js     -> the above + the AutoScroll
    //                                                  and OnSpill *constructors*
    //   sortablejs/modular/sortable.complete.esm.js -> Sortable only, all four
    //                                                  plugins mounted
    //
    // The binding imports everything from the package root, which is what the
    // SortableJS docs use: the root entry point already mounts AutoScroll and
    // both OnSpill plugins (so `Scroll`, `RevertOnSpill` and `RemoveOnSpill`
    // work out of the box), and exports the `MultiDrag` and `Swap` factories
    // that `Sortable.mount` still needs. Every symbol comes from one module, so
    // the bundle holds a single copy of Sortable.
    let ModuleName = "sortablejs"

    let PullResult = T<string[]> + T<bool> + T<string>
    let PutResult = T<string[]> + T<bool>

    let Direction =
        Pattern.EnumStrings "Direction" ["vertical"; "horizontal"]

    let SortablePlugin =
        Class "SortablePlugin"

    // `new MultiDrag()` / `new Swap()` are what `Sortable.mount` expects: the
    // factories return the plugin constructors themselves. AutoScroll and
    // OnSpill are already mounted by the entry point, so they are not exposed.
    let MultiDrag =
        Class "MultiDrag"
        |=> Inherits SortablePlugin
        |+> Static [
            Constructor T<unit>
        ]
        |> Import "MultiDrag" ModuleName

    let Swap =
        Class "Swap"
        |=> Inherits SortablePlugin
        |+> Static [
            Constructor T<unit>
        ]
        |> Import "Swap" ModuleName

    // The main class. `Sortable` is the default export of the module.
    let Sortable =
        Class "Sortable"
        |> ImportDefault ModuleName

    /// `{ multiDragElement, index }` — one entry per selected item in the
    /// `oldIndicies`/`newIndicies` arrays of a multi-drag `SortableEvent`.
    /// `index` is only filled in once the drag starts, so it is optional.
    let MultiDragIndex =
        Pattern.Config "MultiDragIndex" {
            Required = []
            Optional = [
                "multiDragElement", T<HTMLElement>
                "index", !? T<int>
            ]
        }

    /// The event object SortableJS passes to its listeners. It is a
    /// `CustomEvent` decorated with the drag details.
    let SortableEvent =
        Pattern.Config "SortableEvent" {
            Required = []
            Optional = [
                "to", T<HTMLElement>
                "from", T<HTMLElement>
                "item", T<HTMLElement>
                "clone", T<HTMLElement>
                "items", !| T<HTMLElement>
                "clones", !| T<HTMLElement>
                "oldIndex", !? T<int>
                "newIndex", !? T<int>
                "oldDraggableIndex", !? T<int>
                "newDraggableIndex", !? T<int>
                "pullMode", T<string> + T<bool>
                "originalEvent", T<Dom.Event>
                "oldIndicies", !| MultiDragIndex
                "newIndicies", !| MultiDragIndex
                "swapItem", T<HTMLElement>
            ]
        }
        |=> Inherits T<Dom.Event>

    /// The first argument of the `onMove` callback.
    let MoveEvent =
        Pattern.Config "MoveEvent" {
            Required = []
            Optional = [
                "dragged", T<HTMLElement>
                "draggedRect", T<DOMRect>
                "related", T<HTMLElement>
                "relatedRect", T<DOMRect>
                "from", T<HTMLElement>
                "to", T<HTMLElement>
                "willInsertAfter", !? T<bool>
            ]
        }
        |=> Inherits T<Dom.Event>

    /// The `group` option: which lists may exchange items.
    let GroupOptions =
        Pattern.Config "GroupOptions" {
            Required = [
                "name", T<string>
            ]
            Optional = [
                "pull",
                    PullResult +
                    (Sortable?``to`` * Sortable?from * T<HTMLElement>?dragEl * SortableEvent?event ^-> PullResult)
                "put",
                    PutResult +
                    (Sortable?``to`` * Sortable?from * T<HTMLElement>?dragEl * SortableEvent?event ^-> PutResult)
                "checkPull",
                    Sortable?sortable * Sortable?activeSortable * T<HTMLElement>?dragEl * SortableEvent?event ^-> PullResult
                "checkPut",
                    Sortable?sortable * Sortable?activeSortable * T<HTMLElement>?dragEl * SortableEvent?event ^-> PutResult
                "revertClone", T<bool>
            ]
        }

    /// The `fallbackOffset` option: the fallback-drag offset from the cursor.
    let FallbackOffset =
        Pattern.Config "FallbackOffset" {
            Required = []
            Optional = [
                "x", T<int>
                "y", T<int>
            ]
        }

    /// The `store` option: persist and restore the order.
    let StoreConfig =
        Pattern.Config "StoreConfig" {
            Required = []
            Optional = [
                "get", Sortable?sortable ^-> !| T<string>
                "set", Sortable?sortable ^-> T<unit>
            ]
        }

    /// Every option of Sortable and of its four plugins (AutoScroll, OnSpill,
    /// MultiDrag, Swap).
    let Options =
        Pattern.Config "Options" {
            Required = []
            Optional = [
                // SortableOptions
                "animation", T<int>
                "chosenClass", T<string>
                "dataIdAttr", T<string>
                "delay", T<int>
                "delayOnTouchOnly", T<bool>
                "direction", Direction + (SortableEvent?evt * T<HTMLElement>?target * T<HTMLElement>?dragEl ^-> Direction)
                "disabled", T<bool>
                "dragClass", T<string>
                "draggable", T<string>
                "dragoverBubble", T<bool>
                "dropBubble", T<bool>
                "emptyInsertThreshold", T<int>
                "easing", T<string>
                "fallbackClass", T<string>
                "fallbackOnBody", T<bool>
                "fallbackTolerance", T<int>
                "fallbackOffset", FallbackOffset.Type
                "filter",
                    T<string> +
                    (Sortable?this * (T<Dom.Event> + T<TouchEvent>)?evt * T<HTMLElement>?target * Sortable?sortable ^-> T<bool>)
                "forceFallback", T<bool>
                "ghostClass", T<string>
                "group", T<string> + GroupOptions
                "handle", T<string>
                "ignore", T<string>
                "invertSwap", T<bool>
                "invertedSwapThreshold", T<float>
                "preventOnFilter", T<bool>
                "removeCloneOnHide", T<bool>
                "sort", T<bool>
                "store", StoreConfig.Type
                "supportPointer", T<bool>
                "swapThreshold", T<float>
                "touchStartThreshold", T<int>
                "setData", (T<obj>?dataTransfer * T<HTMLElement>?draggedElement ^-> T<unit>)
                "onStart", SortableEvent?event ^-> T<unit>
                "onEnd", SortableEvent?event ^-> T<unit>
                "onAdd", SortableEvent?event ^-> T<unit>
                "onClone", SortableEvent?event ^-> T<unit>
                "onChoose", SortableEvent?event ^-> T<unit>
                "onUnchoose", SortableEvent?event ^-> T<unit>
                "onUpdate", SortableEvent?event ^-> T<unit>
                "onSort", SortableEvent?event ^-> T<unit>
                "onRemove", SortableEvent?event ^-> T<unit>
                "onFilter", SortableEvent?event ^-> T<unit>
                "onMove", MoveEvent?evt * T<Dom.Event>?originalEvent ^-> (T<bool> + T<int> + T<unit>)
                "onChange", SortableEvent?event ^-> T<unit>

                // AutoScrollOptions
                "scroll", T<bool> + T<HTMLElement>
                "forceAutoScrollFallback", T<bool>
                "scrollFn",
                    (Sortable?this * T<int>?offsetX * T<int>?offsetY * T<Dom.Event>?originalEvent *
                     T<TouchEvent>?touchEvt * T<HTMLElement>?hoverTargetEl ^-> (T<string> + T<unit>))
                "scrollSensitivity", T<int>
                "scrollSpeed", T<int>
                "bubbleScroll", T<bool>

                // OnSpillOptions
                "revertOnSpill", T<bool>
                "removeOnSpill", T<bool>
                "onSpill", SortableEvent?evt ^-> T<unit>

                // MultiDragOptions
                "multiDrag", T<bool>
                "selectedClass", T<string>
                "multiDragKey", T<string>
                "avoidImplicitDeselect", T<bool>
                "onSelect", SortableEvent?event ^-> T<unit>
                "onDeselect", SortableEvent?event ^-> T<unit>

                // SwapOptions
                "swap", T<bool>
                "swapClass", T<string>
            ]
        }

    /// The object exposed as `Sortable.utils`: the library's DOM helpers.
    let Utils =
        Class "Utils"
        |+> Instance [
            "on" => (T<HTMLElement>?element * T<string>?eventName * (T<Dom.Event>?evt ^-> T<unit>)?fn ^-> T<unit>)
            "off" => (T<HTMLElement>?element * T<string>?eventName * (T<Dom.Event>?evt ^-> T<unit>)?fn ^-> T<unit>)

            "css" => (T<HTMLElement>?element * T<string>?prop ^-> T<obj>)
            "css" => (T<HTMLElement>?element * T<string>?prop * T<string>?value ^-> T<unit>)
            "css" => (T<HTMLElement>?element ^-> T<CSSStyleDeclaration>)

            "find" =>
                (T<HTMLElement>?context * T<string>?tagName *
                 !?(T<HTMLElement>?value * T<int>?index ^-> T<unit>)?iterator
                 ^-> !| T<HTMLElement>)

            "is" => (T<HTMLElement>?element * T<string>?selector ^-> T<bool>)

            "closest" =>
                (T<HTMLElement>?element * T<string>?selector *
                 !?T<HTMLElement>?context * !?T<bool>?includeContext
                 ^-> T<HTMLElement>)

            "toggleClass" => (T<HTMLElement>?element * T<string>?className * T<bool>?state ^-> T<unit>)

            "extend" => (T<obj>?destination * T<obj>?source ^-> T<obj>)
            "throttle" => ((T<unit> ^-> T<unit>)?callback * T<int>?ms ^-> T<obj>)

            "clone" => (T<HTMLElement>?element ^-> T<HTMLElement>)
            "index" => (T<HTMLElement>?element * !?T<string>?selector ^-> T<int>)

            "nextTick" => ((T<unit> ^-> T<unit>)?fn ^-> T<int>)
            "cancelNextTick" => (T<int>?id ^-> T<unit>)

            "detectDirection" => (T<HTMLElement>?element * Options?options ^-> Direction)

            "getChild" =>
                (T<HTMLElement>?element * T<int>?childNum * !?Options?options * !?T<bool>?includeDragEl
                 ^-> T<HTMLElement>)

            // Added by the MultiDrag plugin when it is mounted.
            "select" => (T<HTMLElement>?element ^-> T<unit>)
            "deselect" => (T<HTMLElement>?element ^-> T<unit>)

            "expando" =@ T<string>
        ]

    Sortable
        |+> Instance [
            "options" =@ Options
            "el" =@ T<HTMLElement>

            // Get or set a single option by name. The getter returns the raw
            // JavaScript value: use `As<'T>` to recover its type.
            "option" => (T<string>?name * T<obj>?value ^-> T<unit>)
            "option" => (T<string>?name ^-> T<obj>)

            "closest" => (T<HTMLElement>?element * !?T<string>?selector ^-> T<HTMLElement>)
            "sort" => (!| T<string>)?order * !?T<bool>?useAnimation ^-> T<unit>
            "save" => T<unit> ^-> T<unit>
            "destroy" => T<unit> ^-> T<unit>
            "toArray" => T<unit> ^-> !| T<string>
        ]
        |+> Static [
            Constructor (T<HTMLElement>?element * !?Options?options)

            "active" =@ TSelf
            "utils" =@ Utils
            "dragged" =@ T<HTMLElement>
            "ghost" =@ T<HTMLElement>
            "clone" =@ T<HTMLElement>
            "version" =@ T<string>

            // Variadic: `Sortable.mount(plugin1, plugin2, ...)`. AutoScroll and
            // both OnSpill plugins are already mounted, so this is only needed
            // for MultiDrag and Swap.
            "mount" => !+ SortablePlugin ^-> T<unit>

            "create" => T<HTMLElement>?element * !?Options?options ^-> TSelf
            "get" => T<HTMLElement>?element ^-> TSelf
        ]
        |> ignore

    let Assembly =
        Assembly [
            Namespace "WebSharper.SortableJs" [
                Options
                Utils
                StoreConfig
                FallbackOffset
                GroupOptions
                MoveEvent
                SortableEvent
                MultiDragIndex
                SortablePlugin
                Sortable
                Swap
                MultiDrag
                Direction
            ]
        ]

[<Sealed>]
type Extension() =
    interface IExtension with
        member ext.Assembly =
            Definition.Assembly

[<assembly: Extension(typeof<Extension>)>]
do ()