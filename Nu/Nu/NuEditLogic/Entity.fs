﻿module NuEditLogic.Entity
open System
open System.Reflection
open Nu.Core
open Nu.Entity
open Nu.Simulation

type EntityModelPropertyChange =
    { Address : Address
      PropertyInfo : PropertyInfo
      Value : obj }

let getEntityModelTypes (entityModel : EntityModel) =
    match entityModel with
    | Button _ -> [typeof<Button>; typeof<Gui>; typeof<Entity>]
    | Label _ -> [typeof<Label>; typeof<Gui>; typeof<Entity>]
    | TextBox _ -> [typeof<TextBox>; typeof<Gui>; typeof<Entity>]
    | Toggle _ -> [typeof<Toggle>; typeof<Gui>; typeof<Entity>]
    | Feeler _ -> [typeof<Feeler>; typeof<Gui>; typeof<Entity>]
    | Block _ -> [typeof<Block>; typeof<Actor>; typeof<Entity>]
    | Avatar _ -> [typeof<Avatar>; typeof<Actor>; typeof<Entity>]
    | TileMap _ -> [typeof<TileMap>; typeof<Actor>; typeof<Entity>]

let getTGuiEntityProperty<'t> (property : PropertyInfo) (entityModel : EntityModel) (tLens : FSharpx.Lens<EntityModel, 'a>) =
    if typeof<'t>.GetProperty (property.Name, BindingFlags.Instance ||| BindingFlags.Public) = property
    then property.GetValue (get entityModel tLens)
    else
        if typeof<Gui>.GetProperty (property.Name, BindingFlags.Instance ||| BindingFlags.Public) = property
        then property.GetValue (get entityModel EntityModel.gui)
        else property.GetValue (get entityModel EntityModel.entity)

let getTActorEntityProperty<'t> (property : PropertyInfo) (entityModel : EntityModel) tLens =
    if typeof<'t>.GetProperty (property.Name, BindingFlags.Instance ||| BindingFlags.Public) = property
    then property.GetValue (get entityModel tLens)
    else
        if typeof<Actor>.GetProperty (property.Name, BindingFlags.Instance ||| BindingFlags.Public) = property
        then property.GetValue (get entityModel EntityModel.actor)
        else property.GetValue (get entityModel EntityModel.entity)

let getEntityModelPropertyValue (property : PropertyInfo) (entityModel : EntityModel) =
    // TODO: fix this awful, AWFUL code duplication!
    match entityModel with
    | Button button ->
        if typeof<Button>.GetProperty (property.Name, BindingFlags.Instance ||| BindingFlags.Public) = property
        then property.GetValue (get entityModel EntityModel.button)
        else
            if typeof<Gui>.GetProperty (property.Name, BindingFlags.Instance ||| BindingFlags.Public) = property
            then property.GetValue (get entityModel EntityModel.gui)
            else property.GetValue (get entityModel EntityModel.entity)
    | Label label ->
        if typeof<Label>.GetProperty (property.Name, BindingFlags.Instance ||| BindingFlags.Public) = property
        then property.GetValue (get entityModel EntityModel.label)
        else
            if typeof<Gui>.GetProperty (property.Name, BindingFlags.Instance ||| BindingFlags.Public) = property
            then property.GetValue (get entityModel EntityModel.gui)
            else property.GetValue (get entityModel EntityModel.entity)
    | TextBox textBox ->
        if typeof<TextBox>.GetProperty (property.Name, BindingFlags.Instance ||| BindingFlags.Public) = property
        then property.GetValue (get entityModel EntityModel.textBox)
        else
            if typeof<Gui>.GetProperty (property.Name, BindingFlags.Instance ||| BindingFlags.Public) = property
            then property.GetValue (get entityModel EntityModel.gui)
            else property.GetValue (get entityModel EntityModel.entity)
    | Toggle toggle ->
        if typeof<Toggle>.GetProperty (property.Name, BindingFlags.Instance ||| BindingFlags.Public) = property
        then property.GetValue (get entityModel EntityModel.toggle)
        else
            if typeof<Gui>.GetProperty (property.Name, BindingFlags.Instance ||| BindingFlags.Public) = property
            then property.GetValue (get entityModel EntityModel.gui)
            else property.GetValue (get entityModel EntityModel.entity)
    | Feeler feeler ->
        if typeof<Feeler>.GetProperty (property.Name, BindingFlags.Instance ||| BindingFlags.Public) = property
        then property.GetValue (get entityModel EntityModel.feeler)
        else
            if typeof<Gui>.GetProperty (property.Name, BindingFlags.Instance ||| BindingFlags.Public) = property
            then property.GetValue (get entityModel EntityModel.gui)
            else property.GetValue (get entityModel EntityModel.entity)
    | Block block ->
        if typeof<Block>.GetProperty (property.Name, BindingFlags.Instance ||| BindingFlags.Public) = property
        then property.GetValue (get entityModel EntityModel.block)
        else
            if typeof<Gui>.GetProperty (property.Name, BindingFlags.Instance ||| BindingFlags.Public) = property
            then property.GetValue (get entityModel EntityModel.gui)
            else property.GetValue (get entityModel EntityModel.entity)
    | Avatar avatar ->
        if typeof<Avatar>.GetProperty (property.Name, BindingFlags.Instance ||| BindingFlags.Public) = property
        then property.GetValue (get entityModel EntityModel.avatar)
        else
            if typeof<Gui>.GetProperty (property.Name, BindingFlags.Instance ||| BindingFlags.Public) = property
            then property.GetValue (get entityModel EntityModel.gui)
            else property.GetValue (get entityModel EntityModel.entity)
    | TileMap tileMap ->
        if typeof<TileMap>.GetProperty (property.Name, BindingFlags.Instance ||| BindingFlags.Public) = property
        then property.GetValue (get entityModel EntityModel.tileMap)
        else
            if typeof<Gui>.GetProperty (property.Name, BindingFlags.Instance ||| BindingFlags.Public) = property
            then property.GetValue (get entityModel EntityModel.gui)
            else property.GetValue (get entityModel EntityModel.entity)

let setEntityModelPropertyValue world (change : EntityModelPropertyChange) =
    let entityModelLens = World.entityModel change.Address
    let entityModel = get world entityModelLens
    let entityModel_ =
        // TODO: so much code duplication, make me wanna slap your momma!
        match entityModel with
        | Button button ->
            let button_ = { button with Gui = button.Gui } // NOTE: this is just a hacky way to copy a record in lieu of reflection
            if typeof<Button>.GetProperty (change.PropertyInfo.Name, BindingFlags.Instance ||| BindingFlags.Public) = change.PropertyInfo
            then let _ = change.PropertyInfo.SetValue (button_, change.Value) in Button button_
            else
                let gui_ = { button_.Gui with Position = button_.Gui.Position } // NOTE: hacky copy
                if typeof<Gui>.GetProperty (change.PropertyInfo.Name, BindingFlags.Instance ||| BindingFlags.Public) = change.PropertyInfo
                then let _ = change.PropertyInfo.SetValue (gui_, change.Value) in Button { button_ with Gui = gui_ }
                else
                    let entity_ = { gui_.Entity with Id = gui_.Entity.Id } // NOTE: hacky copy
                    change.PropertyInfo.SetValue (entity_, change.Value)
                    Button { button_ with Gui = { gui_ with Entity = entity_ }}
        | Label label ->
            let label_ = { label with Gui = label.Gui } // NOTE: hacky copy
            if typeof<Button>.GetProperty (change.PropertyInfo.Name, BindingFlags.Instance ||| BindingFlags.Public) = change.PropertyInfo
            then let _ = change.PropertyInfo.SetValue (label_, change.Value) in Label label_
            else
                let gui_ = { label_.Gui with Position = label_.Gui.Position } // NOTE: hacky copy
                if typeof<Gui>.GetProperty (change.PropertyInfo.Name, BindingFlags.Instance ||| BindingFlags.Public) = change.PropertyInfo
                then let _ = change.PropertyInfo.SetValue (gui_, change.Value) in Label { label_ with Gui = gui_ }
                else
                    let entity_ = { gui_.Entity with Id = gui_.Entity.Id } // NOTE: hacky copy
                    change.PropertyInfo.SetValue (entity_, change.Value)
                    Label { label_ with Gui = { gui_ with Entity = entity_ }}
        | TextBox textBox ->
            let textBox_ = { textBox with Gui = textBox.Gui } // NOTE: hacky copy
            if typeof<TextBox>.GetProperty (change.PropertyInfo.Name, BindingFlags.Instance ||| BindingFlags.Public) = change.PropertyInfo
            then let _ = change.PropertyInfo.SetValue (textBox_, change.Value) in TextBox textBox_
            else
                let gui_ = { textBox_.Gui with Position = textBox_.Gui.Position } // NOTE: hacky copy
                if typeof<Gui>.GetProperty (change.PropertyInfo.Name, BindingFlags.Instance ||| BindingFlags.Public) = change.PropertyInfo
                then let _ = change.PropertyInfo.SetValue (gui_, change.Value) in TextBox { textBox_ with Gui = gui_ }
                else
                    let entity_ = { gui_.Entity with Id = gui_.Entity.Id } // NOTE: hacky copy
                    change.PropertyInfo.SetValue (entity_, change.Value)
                    TextBox { textBox_ with Gui = { gui_ with Entity = entity_ }}
        | Toggle toggle ->
            let toggle_ = { toggle with Gui = toggle.Gui } // NOTE: hacky copy
            if typeof<Toggle>.GetProperty (change.PropertyInfo.Name, BindingFlags.Instance ||| BindingFlags.Public) = change.PropertyInfo
            then let _ = change.PropertyInfo.SetValue (toggle_, change.Value) in Toggle toggle_
            else
                let gui_ = { toggle_.Gui with Position = toggle_.Gui.Position } // NOTE: hacky copy
                if typeof<Gui>.GetProperty (change.PropertyInfo.Name, BindingFlags.Instance ||| BindingFlags.Public) = change.PropertyInfo
                then let _ = change.PropertyInfo.SetValue (gui_, change.Value) in Toggle { toggle_ with Gui = gui_ }
                else
                    let entity_ = { gui_.Entity with Id = gui_.Entity.Id } // NOTE: hacky copy
                    change.PropertyInfo.SetValue (entity_, change.Value)
                    Toggle { toggle_ with Gui = { gui_ with Entity = entity_ }}
        | Feeler feeler ->
            let feeler_ = { feeler with Gui = feeler.Gui } // NOTE: hacky copy
            if typeof<Feeler>.GetProperty (change.PropertyInfo.Name, BindingFlags.Instance ||| BindingFlags.Public) = change.PropertyInfo
            then let _ = change.PropertyInfo.SetValue (feeler_, change.Value) in Feeler feeler_
            else
                let gui_ = { feeler_.Gui with Position = feeler_.Gui.Position } // NOTE: hacky copy
                if typeof<Gui>.GetProperty (change.PropertyInfo.Name, BindingFlags.Instance ||| BindingFlags.Public) = change.PropertyInfo
                then let _ = change.PropertyInfo.SetValue (gui_, change.Value) in Feeler { feeler_ with Gui = gui_ }
                else
                    let entity_ = { gui_.Entity with Id = gui_.Entity.Id } // NOTE: hacky copy
                    change.PropertyInfo.SetValue (entity_, change.Value)
                    Feeler { feeler_ with Gui = { gui_ with Entity = entity_ }}
        | Block block ->
            let block_ = { block with Actor = block.Actor } // NOTE: hacky copy
            if typeof<Block>.GetProperty (change.PropertyInfo.Name, BindingFlags.Instance ||| BindingFlags.Public) = change.PropertyInfo
            then let _ = change.PropertyInfo.SetValue (block_, change.Value) in Block block_
            else
                let actor_ = { block_.Actor with Position = block_.Actor.Position } // NOTE: hacky copy
                if typeof<Actor>.GetProperty (change.PropertyInfo.Name, BindingFlags.Instance ||| BindingFlags.Public) = change.PropertyInfo
                then let _ = change.PropertyInfo.SetValue (actor_, change.Value) in Block { block_ with Actor = actor_ }
                else
                    let entity_ = { actor_.Entity with Id = actor_.Entity.Id } // NOTE: hacky copy
                    change.PropertyInfo.SetValue (entity_, change.Value)
                    Block { block_ with Actor = { actor_ with Entity = entity_ }}
        | Avatar avatar ->
            let avatar_ = { avatar with Actor = avatar.Actor } // NOTE: hacky copy
            if typeof<Avatar>.GetProperty (change.PropertyInfo.Name, BindingFlags.Instance ||| BindingFlags.Public) = change.PropertyInfo
            then let _ = change.PropertyInfo.SetValue (avatar_, change.Value) in Avatar avatar_
            else
                let actor_ = { avatar_.Actor with Position = avatar_.Actor.Position } // NOTE: hacky copy
                if typeof<Actor>.GetProperty (change.PropertyInfo.Name, BindingFlags.Instance ||| BindingFlags.Public) = change.PropertyInfo
                then let _ = change.PropertyInfo.SetValue (actor_, change.Value) in Avatar { avatar_ with Actor = actor_ }
                else
                    let entity_ = { actor_.Entity with Id = actor_.Entity.Id } // NOTE: hacky copy
                    change.PropertyInfo.SetValue (entity_, change.Value)
                    Avatar { avatar_ with Actor = { actor_ with Entity = entity_ }}
        | TileMap tileMap ->
            let tileMap_ = { tileMap with Actor = tileMap.Actor } // NOTE: hacky copy
            if typeof<TileMap>.GetProperty (change.PropertyInfo.Name, BindingFlags.Instance ||| BindingFlags.Public) = change.PropertyInfo
            then let _ = change.PropertyInfo.SetValue (tileMap_, change.Value) in TileMap tileMap_
            else
                let actor_ = { tileMap_.Actor with Position = tileMap_.Actor.Position } // NOTE: hacky copy
                if typeof<Actor>.GetProperty (change.PropertyInfo.Name, BindingFlags.Instance ||| BindingFlags.Public) = change.PropertyInfo
                then let _ = change.PropertyInfo.SetValue (actor_, change.Value) in TileMap { tileMap_ with Actor = actor_ }
                else
                    let entity_ = { actor_.Entity with Id = actor_.Entity.Id } // NOTE: hacky copy
                    change.PropertyInfo.SetValue (entity_, change.Value)
                    TileMap { tileMap_ with Actor = { actor_ with Entity = entity_ }}
    set entityModel_ world entityModelLens

let setEntityModelPropertyValues changes world =
    Seq.fold setEntityModelPropertyValue world changes