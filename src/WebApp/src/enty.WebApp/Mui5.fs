namespace Feliz.MaterialUI.Mui5

open Fable.Core.JsInterop
open Feliz
open Feliz.MaterialUI

module MuiHelpers =
    let inline createElementImportDefault path = createElement (importDefault path)
    let mkAttr = Interop.mkAttr
open MuiHelpers


type stack =
    static member inline spacing(value: int) = mkAttr "spacing" value
    static member inline children(elements: ReactElement seq) = prop.children elements
module stack =
    type direction =
        static member inline row = mkAttr "direction" "row"
        static member inline column = mkAttr "direction" "column"

[<RequireQualifiedAccess>]
type Mui =
    static member inline stack(props) = createElementImportDefault "@mui/material/Stack" props
    static member inline stack(children: ReactElement seq) = createElementImportDefault "@mui/material/Stack" [ stack.children children ]

    static member inline textField(props) = createElementImportDefault "@mui/material/TextField" props

    static member inline button(props) = createElementImportDefault "@mui/material/Button" props
    static member inline button(children: ReactElement seq) = createElementImportDefault "@mui/material/Button" [ button.children children ]

    static member inline paper(props) = createElementImportDefault "@mui/material/Paper" props
    static member inline paper(children: ReactElement seq) = createElementImportDefault "@mui/material/Paper" [ paper.children children ]

    static member inline container(props) = createElementImportDefault "@mui/material/Container" props
    static member inline container(children: ReactElement seq) = createElementImportDefault "@mui/material/Container" [ container.children children ]

    static member inline grid(props) = createElementImportDefault "@mui/material/Grid" props
    static member inline grid(children: ReactElement seq) = createElementImportDefault "@mui/material/Grid" [ grid.children children ]

    static member inline chip(props) = createElementImportDefault "@mui/material/Chip" props

    static member inline switch(props) = createElementImportDefault "@mui/material/Switch" props

    static member inline formGroup(props) = createElementImportDefault "@mui/material/FormGroup" props
    static member inline formGroup(children: ReactElement seq) = createElementImportDefault "@mui/material/FormGroup" [ formGroup.children children ]

    static member inline formControlLabel(props) = createElementImportDefault "@mui/material/FormControlLabel" props

