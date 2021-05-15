namespace enty.WebApp.EntityRendering

open Fable.React
open enty.Core

type IEntityRenderer =
    abstract TryRender: Entity -> ReactElement option

