﻿// Nu Game Engine.
// Copyright (C) Bryan Edds, 2013-2020.

namespace OmniBlade
open System
open Prime
open Nu
open Nu.Declarative

type [<StructuralEquality; NoComparison>] DialogForm =
    | DialogThin    
    | DialogThick

type [<ReferenceEquality; NoComparison>] Dialog =
    { DialogForm : DialogForm
      DialogTokenized : string
      DialogProgress : int
      DialogPage : int
      DialogPromptOpt : ((string * Cue) * (string * Cue)) option
      DialogBattleOpt : (BattleType * Advent Set) option }

    static member update dialog world =
        let increment = if World.getTickTime world % 2L = 0L then 1 else 0
        let dialog = { dialog with DialogProgress = dialog.DialogProgress + increment }
        dialog

    static member canAdvance (detokenize : string -> string) dialog =
        let detokenized = detokenize dialog.DialogTokenized
        dialog.DialogProgress > detokenized.Split(Constants.Gameplay.DialogSplit).[dialog.DialogPage].Length

    static member tryAdvance (detokenize : string -> string) dialog =
        let detokenized = detokenize dialog.DialogTokenized
        if dialog.DialogPage < detokenized.Split(Constants.Gameplay.DialogSplit).Length - 1 then
            let dialog = { dialog with DialogProgress = 0; DialogPage = inc dialog.DialogPage }
            (true, dialog)
        else (false, dialog)

    static member isExhausted (detokenize : string -> string) dialog =
        let detokenized = detokenize dialog.DialogTokenized
        let pages = detokenized.Split Constants.Gameplay.DialogSplit
        let lastPage = dec (Array.length pages)
        dialog.DialogPage = lastPage &&
        dialog.DialogProgress > pages.[lastPage].Length

    static member content name elevation promptLeft promptRight (detokenizeAndDialogOpt : Lens<(string -> string) * Dialog option, World>) =
        Content.entityWithContent<TextDispatcher> name
            [Entity.Bounds <== detokenizeAndDialogOpt --> fun (_, dialogOpt) ->
                match dialogOpt with
                | Some dialog ->
                    match dialog.DialogForm with
                    | DialogThin -> v4Bounds (v2 -432.0f 150.0f) (v2 864.0f 90.0f)
                    | DialogThick -> v4Bounds (v2 -432.0f 60.0f) (v2 864.0f 192.0f)
                | None -> v4Zero
             Entity.Elevation == elevation
             Entity.BackgroundImageOpt <== detokenizeAndDialogOpt --> fun (_, dialogOpt) ->
                let image =
                    match dialogOpt with
                    | Some dialog ->
                        match dialog.DialogForm with
                        | DialogThin -> Assets.Gui.DialogThinImage
                        | DialogThick -> Assets.Gui.DialogThickImage
                    | None -> Assets.Gui.DialogThickImage
                Some image
             Entity.Text <== detokenizeAndDialogOpt --> fun (detokenize, dialogOpt) ->
                match dialogOpt with
                | Some dialog ->
                    let detokenized = detokenize dialog.DialogTokenized
                    let textPage = dialog.DialogPage
                    let text = detokenized.Split(Constants.Gameplay.DialogSplit).[textPage]
                    let textToShow = String.tryTake dialog.DialogProgress text
                    textToShow
                | None -> ""
             Entity.Justification == Unjustified true
             Entity.Margins == v2 32.0f 32.0f]
            [Content.button (name + "+Left")
                [Entity.PositionLocal == v2 198.0f 42.0f; Entity.ElevationLocal == 2.0f
                 Entity.VisibleLocal <== detokenizeAndDialogOpt --> fun (detokenize, dialogOpt) ->
                    match dialogOpt with
                    | Some dialog -> Option.isSome dialog.DialogPromptOpt && Dialog.isExhausted detokenize dialog
                    | None -> false
                 Entity.Text <== detokenizeAndDialogOpt --> fun (_, dialogOpt) ->
                    match dialogOpt with
                    | Some dialog -> match dialog.DialogPromptOpt with Some ((promptText, _), _) -> promptText | None -> ""
                    | None -> ""
                 Entity.ClickEvent ==> msg promptLeft]
             Content.button (name + "+Right")
                [Entity.PositionLocal == v2 498.0f 42.0f; Entity.ElevationLocal == 2.0f
                 Entity.VisibleLocal <== detokenizeAndDialogOpt --> fun (detokenize, dialogOpt) ->
                    match dialogOpt with
                    | Some dialog -> Option.isSome dialog.DialogPromptOpt && Dialog.isExhausted detokenize dialog
                    | None -> false
                 Entity.Text <== detokenizeAndDialogOpt --> fun (_, dialogOpt) ->
                     match dialogOpt with
                     | Some dialog -> match dialog.DialogPromptOpt with Some (_, (promptText, _)) -> promptText | None -> ""
                     | None -> ""
                 Entity.ClickEvent ==> msg promptRight]]