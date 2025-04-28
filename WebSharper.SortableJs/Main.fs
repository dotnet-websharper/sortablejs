namespace WebSharper.SortableJs

open WebSharper
open WebSharper.JavaScript
open WebSharper.InterfaceGenerator
open WebSharper.TouchEvents

module Definition =

    let PullResult = T<string[]> + T<bool> + T<string>
    let PutResult = T<string[]> + T<bool> + T<string>

    let Direction = 
        Pattern.EnumStrings "Direction" ["vertical"; "horizontal"]

    let AutoScroll =
        Class "AutoScroll"
        |+> Static [
            Constructor T<unit>
        ]
        |> Import "AutoScroll" "sortablejs"

    let OnSpill =
        Class "OnSpill"
        |+> Static [
            Constructor T<unit>
        ]
        |> Import "OnSpill" "sortablejs"

    let MultiDrag = 
        Class "MultiDrag"
        |+> Static [
            Constructor T<unit>
        ]
        |> Import "MultiDrag" "sortablejs"

    let Swap =
        Class "Swap"
        |+> Static [
            Constructor T<unit>
        ]
        |> Import "Swap" "sortablejs"

    let SortablePlugin = AutoScroll + OnSpill + MultiDrag + Swap

    let Sortable =
        Class "Sortable"
        |> ImportDefault "sortablejs"

    let MultiDragIndex =
        Pattern.Config "MultiDragIndex" {
            Required = []
            Optional = [
                "multiDragElement", T<HTMLElement>
                "index", T<int>
            ]
        }

    let SortableEvent =
        Pattern.Config "SortableEvent" {
            Required = []
            Optional = [
                "clone", T<HTMLElement>
                "from", T<HTMLElement>
                "item", T<HTMLElement>
                "items", !|T<HTMLElement>
                "newIndex", !?T<int>
                "oldIndex", !?T<int>
                "target", T<HTMLElement>
                "to", T<HTMLElement>
                "oldDraggableIndex", !?T<int>
                "newDraggableIndex", !?T<int>
                "pullMode", T<string> + T<bool>
                "oldIndicies", !| MultiDragIndex
                "newIndicies", !| MultiDragIndex
                "swapItem", !?T<HTMLElement>
            ]
        }
        |=> Inherits T<Dom.Event>  
        |> Import "SortableEvent" "sortablejs"

    let MoveEvent =
        Pattern.Config "MoveEvent" {
            Required = []
            Optional = [
                "dragged", T<HTMLElement>
                "draggedRect", T<DOMRect>
                "from", T<HTMLElement>
                "related", T<HTMLElement>
                "relatedRect", T<DOMRect>
                "to", T<HTMLElement>
                "willInsertAfter", !?T<bool>
            ]
        }
        |> Import "MoveEvent" "sortablejs"

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
        |> Import "GroupOptions" "sortablejs"

    let FallbackOffset =
        Pattern.Config "FallbackOffset" {
            Required = []
            Optional = [
                "x", T<int>
                "y", T<int>
            ]
        }

    let StoreConfig =
        Pattern.Config "StoreConfig" {
            Required = []
            Optional = [
                "get", Sortable?sortable ^-> !| T<string>
                "set", Sortable?sortable ^-> T<unit>
            ]
        }

    let Utils =

        let callbackFn = T<Dom.Event>?evt ^-> T<unit>

        Pattern.Config "Utils" {
            Required = []
            Optional = [
                "on", T<HTMLElement>?element * T<string>?eventName * callbackFn?fn ^-> T<unit>
                "off", T<HTMLElement>?element * T<string>?eventName * callbackFn?fn ^-> T<unit>

                "css", T<HTMLElement>?element * !?T<string>?prop * !?T<CSSStyleDeclaration>?value ^-> T<unit> + T<CSSStyleDeclaration>

                "find", 
                    T<HTMLElement>?context * T<string>?tagName * 
                    !?(T<HTMLElement>?value * T<int>?index ^-> T<unit>)?iterator
                    ^-> !| T<HTMLElement>

                "is", T<HTMLElement>?element * T<string>?selector ^-> T<bool>

                "closest", 
                    T<HTMLElement>?element * T<string>?selector * 
                    !?T<HTMLElement>?context
                    ^-> T<HTMLElement>

                "toggleClass", T<HTMLElement>?element * T<string>?className * T<bool>?state ^-> T<unit>

                "select", T<HTMLElement>?element ^-> T<unit>
                "deselect", T<HTMLElement>?element ^-> T<unit>
            ]
        }

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
                "scroll", (T<bool> + T<HTMLElement>)
                "forceAutoScrollFallback", T<bool>
                "scrollFn", (Sortable?this * T<int>?offsetX * T<int>?offsetY * T<Dom.Event>?originalEvent * T<TouchEvent>?touchEvt * T<HTMLElement>?hoverTargetEl ^-> (T<string> + T<unit>))
                "scrollSensitivity", T<int>
                "scrollSpeed", T<int>
                "bubbleScroll", T<bool>

                // OnSpillOptions
                "revertOnSpill", T<bool>
                "removeOnSpill", T<bool>
                "onSpill", (SortableEvent?evt ^-> T<unit>)

                // MultiDragOptions
                "multiDrag", T<bool>
                "selectedClass", T<string>
                "multiDragKey", T<string>
                "avoidImplicitDeselect", T<bool>
                "onSelect", (SortableEvent?event ^-> T<unit>)
                "onDeselect", (SortableEvent?event ^-> T<unit>)

                // SwapOptions
                "swap", T<bool>
                "swapClass", T<string>
            ]
        }
        |> Import "Options" "sortablejs"

    Sortable
        |+> Instance [
            "options" =@ Options 
            "el" =@ T<HTMLElement>

            "option" => (T<string>?name * Options?value ^-> T<unit>)
            "option" => (T<string>?name ^-> Options)
        
            "closest" => T<HTMLElement>?element * !?T<string>?selector ^-> T<HTMLElement>
            "sort" => (!|T<string>)?order * !?T<bool>?useAnimation ^-> T<unit>
            "save" => T<unit> ^-> T<unit>
            "destroy" => T<unit> ^-> T<unit>
            "toArray" => T<unit> ^-> !|T<string>
        ]
        |+> Static [
            Constructor (T<HTMLElement>?element * Options?options * TSelf?sortable)

            "active" =@ TSelf 
            "utils" =@ Utils 

            "mount" => !|SortablePlugin ^-> T<unit> 
            "create" => T<HTMLElement>?element * !?Options?options ^-> TSelf
            "dragged" =@ T<HTMLElement>
            "ghost" =@ T<HTMLElement>
            "clone" =@ T<HTMLElement>
            "get" => T<HTMLElement>?element ^-> TSelf 
            "version" =@ T<string>
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
                Sortable
                Swap
                MultiDrag
                OnSpill
                AutoScroll
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
