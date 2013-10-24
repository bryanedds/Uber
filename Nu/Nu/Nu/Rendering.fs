﻿module Nu.Rendering
open System
open System.IO
open OpenTK
open SDL2
open Nu.Core
open Nu.Constants
open Nu.Assets

type [<StructuralEquality; NoComparison>] Sprite =
    { SpriteAssetName : Lun
      PackageName : Lun
      PackageFileName : string }

type [<StructuralEquality; NoComparison>] SpriteDescriptor =
    { Position : Vector2
      Size : Vector2
      Rotation : single
      Sprite : Sprite }

type [<StructuralEquality; NoComparison>] TileMapAsset =
    { TileMapAssetName : Lun
      PackageName : Lun
      PackageFileName : string }

type [<StructuralEquality; NoComparison>] TileDescriptor =
    { Position : Vector2
      Size : Vector2
      Rotation : single
      TileSetSprite : Sprite
      TileSetPosition : Vector2 }

type [<StructuralEquality; NoComparison>] Font =
    { FontAssetName : Lun
      PackageName : Lun
      PackageFileName : string }

type [<StructuralEquality; NoComparison>] TextDescriptor =
    { Text : string
      Position : Vector2
      Size : Vector2
      Font : Font
      Color : Vector4 }

type [<StructuralEquality; NoComparison>] 'd LayeredDescriptor =
    { Descriptor : 'd
      Depth : single }

type [<StructuralEquality; NoComparison>] LayerableDescriptor =
    | LayeredSpriteDescriptor of SpriteDescriptor LayeredDescriptor
    | LayeredTileDescriptor of TileDescriptor LayeredDescriptor
    | LayeredTextDescriptor of TextDescriptor LayeredDescriptor

/// Describes a rendering asset.
/// A serializable value type.
type [<StructuralEquality; NoComparison>] RenderDescriptor =
    | LayerableDescriptor of LayerableDescriptor

type [<StructuralEquality; NoComparison>] HintRenderingPackageUse =
    { FileName : string
      PackageName : string
      HRPU : unit }

type [<StructuralEquality; NoComparison>] HintRenderingPackageDisuse =
    { FileName : string
      PackageName : string
      HRPD : unit }

type [<StructuralEquality; NoComparison>] RenderMessage =
    | HintRenderingPackageUse of HintRenderingPackageUse
    | HintRenderingPackageDisuse of HintRenderingPackageDisuse
    | ScreenFlash // of ...

type [<ReferenceEquality>] RenderAsset =
    | TextureAsset of nativeint
    | FontAsset of nativeint * int

type [<ReferenceEquality>] Renderer =
    private
        { RenderContext : nativeint
          RenderAssetMap : RenderAsset AssetMap }

let getLayerableDepth layerable =
    match layerable with
    | LayeredSpriteDescriptor descriptor -> descriptor.Depth
    | LayeredTileDescriptor descriptor -> descriptor.Depth
    | LayeredTextDescriptor descriptor -> descriptor.Depth

let freeRenderAsset renderAsset =
    match renderAsset with
    | TextureAsset texture -> SDL.SDL_DestroyTexture texture
    | FontAsset (font, _) -> SDL_ttf.TTF_CloseFont font

let tryLoadRenderAsset2 renderContext (asset : Asset) =
    let extension = Path.GetExtension asset.FileName
    match extension with
    | ".bmp"
    | ".png" ->
        let optTexture = SDL_image.IMG_LoadTexture (renderContext, asset.FileName)
        if optTexture <> IntPtr.Zero then Some (Lun.make asset.Name, TextureAsset optTexture)
        else
            trace ("Could not load texture '" + asset.FileName + "'.")
            None
    | ".ttf" ->
        let fileFirstName = Path.GetFileNameWithoutExtension asset.FileName
        let fileFirstNameLength = fileFirstName.Length
        if fileFirstNameLength >= 3 then
            let fontSizeText = fileFirstName.Substring(fileFirstNameLength - 3, 3)
            let fontSize = ref 0
            if Int32.TryParse (fontSizeText, fontSize) then
                let optFont = SDL_ttf.TTF_OpenFont (asset.FileName, fontSize.Value)
                if optFont <> IntPtr.Zero then Some (Lun.make asset.Name, FontAsset (optFont, fontSize.Value))
                else trace ("Could not load font due to unparsable font size in file name '" + asset.FileName + "'."); None
            else trace ("Could not load font due to file name being too short: '" + asset.FileName + "'."); None
        else trace ("Could not load font '" + asset.FileName + "'."); None
    | _ ->
        trace ("Could not load render asset '" + str asset + "' due to unknown extension '" + extension + "'.")
        None

let tryLoadRenderPackage packageName fileName renderer =
    let optAssets = tryLoadAssets "Rendering" packageName.LunStr fileName
    match optAssets with
    | Left error ->
        trace ("HintRenderingPackageUse failed due unloadable assets '" + error + "' for '" + str (packageName, fileName) + "'.")
        renderer
    | Right assets ->
        let optRenderAssets = List.map (tryLoadRenderAsset2 renderer.RenderContext) assets
        let renderAssets = List.definitize optRenderAssets
        let optRenderAssetMap = Map.tryFind packageName renderer.RenderAssetMap
        match optRenderAssetMap with
        | None ->
            let renderAssetMap = Map.ofSeq renderAssets
            { renderer with RenderAssetMap = Map.add packageName renderAssetMap renderer.RenderAssetMap }
        | Some renderAssetMap ->
            let renderAssetMap2 = Map.addMany renderAssets renderAssetMap
            { renderer with RenderAssetMap = Map.add packageName renderAssetMap2 renderer.RenderAssetMap }

let tryLoadRenderAsset packageName packageFileName assetName renderer =
    let optAssetMap = Map.tryFind packageName renderer.RenderAssetMap
    let (renderer_, optAssetMap_) =
        match optAssetMap with
        | None ->
            log ("Loading render package '" + packageName.LunStr + "' for asset '" + assetName.LunStr + "' on the fly.")
            let renderer_ = tryLoadRenderPackage packageName packageFileName renderer
            (renderer_, Map.tryFind packageName renderer_.RenderAssetMap)
        | Some assetMap -> (renderer, Map.tryFind packageName renderer.RenderAssetMap)
    (renderer_, Option.bind (fun assetMap -> Map.tryFind assetName assetMap) optAssetMap_)

let handleHintRenderingPackageUse (hintPackageUse : HintRenderingPackageUse) renderer =
    tryLoadRenderPackage (Lun.make hintPackageUse.PackageName) hintPackageUse.FileName renderer
    
let handleHintRenderingPackageDisuse (hintPackageDisuse : HintRenderingPackageDisuse) renderer =
    let packageNameLun = Lun.make hintPackageDisuse.PackageName
    let optAssets = Map.tryFind packageNameLun renderer.RenderAssetMap
    match optAssets with
    | None -> renderer
    | Some assets ->
        for asset in Map.toValueList assets do freeRenderAsset asset
        { renderer with RenderAssetMap = Map.remove packageNameLun renderer.RenderAssetMap }

let handleRenderMessage renderer renderMessage =
    match renderMessage with
    | HintRenderingPackageUse hintPackageUse -> handleHintRenderingPackageUse hintPackageUse renderer
    | HintRenderingPackageDisuse hintPackageDisuse -> handleHintRenderingPackageDisuse hintPackageDisuse renderer
    | ScreenFlash -> renderer // TODO: render screen flash for one frame

let handleRenderMessages (renderMessages : RenderMessage rQueue) renderer =
    List.fold handleRenderMessage renderer (List.rev renderMessages)

let handleRenderExit renderer =
    let renderAssetMaps = Map.toValueSeq renderer.RenderAssetMap
    let renderAssets = Seq.collect Map.toValueSeq renderAssetMaps
    for renderAsset in renderAssets do freeRenderAsset renderAsset
    { renderer with RenderAssetMap = Map.empty }

let renderLayerableDescriptor renderer layerableDescriptor =
    match layerableDescriptor with
    | LayeredSpriteDescriptor lsd ->
        let spriteDescriptor = lsd.Descriptor
        let sprite = spriteDescriptor.Sprite
        let (renderer2, optRenderAsset) = tryLoadRenderAsset sprite.PackageName sprite.PackageFileName sprite.SpriteAssetName renderer
        match optRenderAsset with
        | None ->
            debug ("LayeredSpriteDescriptor failed due to unloadable assets for '" + str sprite + "'.")
            renderer2
        | Some renderAsset ->
            match renderAsset with
            | TextureAsset texture ->
                let mutable sourceRect = SDL.SDL_Rect ()
                sourceRect.x <- 0
                sourceRect.y <- 0
                sourceRect.w <- int spriteDescriptor.Size.X
                sourceRect.h <- int spriteDescriptor.Size.Y
                let mutable destRect = SDL.SDL_Rect ()
                destRect.x <- int spriteDescriptor.Position.X
                destRect.y <- int spriteDescriptor.Position.Y
                destRect.w <- int spriteDescriptor.Size.X
                destRect.h <- int spriteDescriptor.Size.Y
                let mutable rotationCenter = SDL.SDL_Point ()
                rotationCenter.x <- int (spriteDescriptor.Size.X * 0.5f)
                rotationCenter.y <- int (spriteDescriptor.Size.Y * 0.5f)
                let renderResult =
                    SDL.SDL_RenderCopyEx
                        (renderer2.RenderContext,
                         texture,
                         ref sourceRect,
                         ref destRect,
                         double spriteDescriptor.Rotation * RadiansToDegrees,
                         ref rotationCenter,
                         SDL.SDL_RendererFlip.SDL_FLIP_NONE)
                if renderResult <> 0 then debug ("Rendering error - could not render texture for sprite '" + str spriteDescriptor + "' due to '" + SDL.SDL_GetError () + ".")
                renderer2
            | _ ->
                trace "Cannot render sprite with a non-texture asset."
                renderer2
    | LayeredTileDescriptor ltd ->
        let tileDescriptor = ltd.Descriptor
        let sprite = tileDescriptor.TileSetSprite
        let (renderer2, optRenderAsset) = tryLoadRenderAsset sprite.PackageName sprite.PackageFileName sprite.SpriteAssetName renderer
        match optRenderAsset with
        | None ->
            debug ("LayeredTileDescriptor failed due to unloadable assets for '" + str sprite + "'.")
            renderer2
        | Some renderAsset ->
            match renderAsset with
            | TextureAsset texture ->
                let mutable sourceRect = SDL.SDL_Rect ()
                sourceRect.x <- int tileDescriptor.TileSetPosition.X
                sourceRect.y <- int tileDescriptor.TileSetPosition.Y
                sourceRect.w <- int tileDescriptor.Size.X
                sourceRect.h <- int tileDescriptor.Size.Y
                let mutable destRect = SDL.SDL_Rect ()
                destRect.x <- int tileDescriptor.Position.X
                destRect.y <- int tileDescriptor.Position.Y
                destRect.w <- int tileDescriptor.Size.X
                destRect.h <- int tileDescriptor.Size.Y
                let mutable rotationCenter = SDL.SDL_Point ()
                rotationCenter.x <- int (tileDescriptor.Size.X * 0.5f)
                rotationCenter.y <- int (tileDescriptor.Size.Y * 0.5f)
                let renderResult =
                    SDL.SDL_RenderCopyEx
                        (renderer2.RenderContext,
                         texture,
                         ref sourceRect,
                         ref destRect,
                         double tileDescriptor.Rotation * RadiansToDegrees,
                         ref rotationCenter,
                         SDL.SDL_RendererFlip.SDL_FLIP_NONE) // TODO: implement tile flip
                if renderResult <> 0 then debug ("Rendering error - could not render texture for tile '" + str tileDescriptor + "' due to '" + SDL.SDL_GetError () + ".")
                renderer2
            | _ ->
                trace "Cannot render tile with a non-texture asset."
                renderer2
    | LayeredTextDescriptor ltd ->
        let textDescriptor = ltd.Descriptor
        let font = textDescriptor.Font
        let (renderer2, optRenderAsset) = tryLoadRenderAsset font.PackageName font.PackageFileName font.FontAssetName renderer
        match optRenderAsset with
        | None ->
            debug ("LayeredTextDescriptor failed due to unloadable assets for '" + str font + "'.")
            renderer2
        | Some renderAsset ->
            match renderAsset with
            | FontAsset (font, _) ->
                let mutable color = SDL.SDL_Color ()
                color.r <- byte (textDescriptor.Color.X * 255.0f)
                color.g <- byte (textDescriptor.Color.Y * 255.0f)
                color.b <- byte (textDescriptor.Color.Z * 255.0f)
                color.a <- byte (textDescriptor.Color.W * 255.0f)
                let mutable sourceRect = SDL.SDL_Rect ()
                sourceRect.x <- 0
                sourceRect.y <- 0
                sourceRect.w <- int textDescriptor.Size.X
                sourceRect.h <- int textDescriptor.Size.Y
                let mutable destRect = SDL.SDL_Rect ()
                destRect.x <- int textDescriptor.Position.X
                destRect.y <- int textDescriptor.Position.Y
                destRect.w <- int textDescriptor.Size.X
                destRect.h <- int textDescriptor.Size.Y
                // NOTE: the following code is not exception safe!
                // TODO: the resource implications (perf and vram fragmentation?) of creating and
                // destroying a texture one or more times a frame must be understood!
                let textSurface = SDL_ttf.TTF_RenderText_Blended_Wrapped (font, textDescriptor.Text, color, uint32 textDescriptor.Size.X)
                if textSurface <> IntPtr.Zero then
                    let textTexture = SDL.SDL_CreateTextureFromSurface (renderer.RenderContext, textSurface)
                    if textTexture <> IntPtr.Zero then ignore (SDL.SDL_RenderCopy (renderer.RenderContext, textTexture, ref sourceRect, ref destRect))
                    SDL.SDL_DestroyTexture textTexture
                    SDL.SDL_FreeSurface textSurface
                renderer2
            | _ ->
                trace "Cannot render text with a non-font asset."
                renderer2

let renderDescriptors renderDescriptorsValue renderer =
    let renderContext = renderer.RenderContext
    let targetResult = SDL.SDL_SetRenderTarget (renderContext, IntPtr.Zero)
    match targetResult with
    | 0 ->
        ignore (SDL.SDL_SetRenderDrawBlendMode (renderContext, SDL.SDL_BlendMode.SDL_BLENDMODE_ADD))
        let layerableDescriptors = Seq.map (fun (LayerableDescriptor descriptor) -> descriptor) renderDescriptorsValue
        //let (spriteDescriptors, renderDescriptorsValue_) = List.partitionPlus (fun descriptor -> match descriptor with SpriteDescriptor spriteDescriptor -> Some spriteDescriptor (*| _ -> None*)) renderDescriptorsValue
        let sortedDescriptors = Seq.sortBy getLayerableDepth layerableDescriptors
        Seq.fold renderLayerableDescriptor renderer sortedDescriptors
    | _ ->
        trace ("Rendering error - could not set render target to display buffer due to '" + SDL.SDL_GetError () + ".")
        renderer

let render (renderMessages : RenderMessage rQueue) renderDescriptorsValue renderer =
    let renderer2 = handleRenderMessages renderMessages renderer
    renderDescriptors renderDescriptorsValue renderer2

let makeRenderer renderContext =
    { RenderContext = renderContext
      RenderAssetMap = Map.empty }