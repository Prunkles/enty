namespace Feliz.MaterialUI.Mui5

open Fable.Core.JsInterop
open Feliz
open Feliz.MaterialUI

module MuiHelpers =
    let inline createElementImportDefault path (props: #seq<IReactProperty>) = createElement (importDefault path) props
    let inline mkAttr key value = Interop.mkAttr key value
open MuiHelpers

// ----

type prop =
    static member inline sx(value: obj) = mkAttr "sx" value

type stack =
    static member inline spacing(value: 'a) = mkAttr "spacing" value
    static member inline children(elements: ReactElement seq) = prop.children elements
module stack =
    type direction =
        static member inline row = mkAttr "direction" "row"
        static member inline column = mkAttr "direction" "column"
    type alignItems =
        static member inline flexStart = mkAttr "alignItems" "flex-start"
        static member inline center = mkAttr "alignItems" "center"
        static member inline flexEnd = mkAttr "alignItems" "flex-end"
        static member inline stretch = mkAttr "alignItems" "stretch"
        static member inline baseline = mkAttr "alignItems" "baseline"

type box =
    static member inline sx(value: obj) = mkAttr "sx" value
    static member inline children(elements: ReactElement seq) = prop.children elements

[<RequireQualifiedAccess>]
type Mui =
    static member inline stack(props) = createElementImportDefault "@mui/material/Stack" props
    static member inline stack(children: ReactElement seq) = createElementImportDefault "@mui/material/Stack" [ stack.children children ]

    static member inline textField(props) = createElementImportDefault "@mui/material/TextField" props

    static member inline button(props) = createElementImportDefault "@mui/material/Button" props
    static member inline button(children: #seq<ReactElement>) = createElementImportDefault "@mui/material/Button" [ button.children (children :> _ seq) ]

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

    static member inline appBar(props) = createElementImportDefault "@mui/material/AppBar" props
    static member inline appBar(children: ReactElement seq) = createElementImportDefault "@mui/material/AppBar" [ appBar.children children ]

    static member inline toolbar(props) = createElementImportDefault "@mui/material/Toolbar" props
    static member inline toolbar(children: ReactElement seq) = createElementImportDefault "@mui/material/Toolbar" [ toolbar.children children ]

    static member inline typography(props) = createElementImportDefault "@mui/material/Typography" props
    static member inline typography(children: ReactElement seq) = createElementImportDefault "@mui/material/Typography" [ typography.children children ]

    static member inline box(props) = createElementImportDefault "@mui/material/Box" props
    static member inline box(children: #seq<ReactElement>) = createElementImportDefault "@mui/material/Box" [ box.children children ]

    static member inline cssBaseline(props) = createElementImportDefault "@mui/material/CssBaseline" props
    static member inline cssBaseline(children: #seq<ReactElement>) = createElementImportDefault "@mui/material/CssBaseline" [ cssBaseline.children (children :> _ seq) ]

    static member inline card(props) = createElementImportDefault "@mui/material/Card" props
    static member inline card(children: #seq<ReactElement>) = createElementImportDefault "@mui/material/Card" [ card.children (children :> _ seq) ]

    static member inline cardContent(props) = createElementImportDefault "@mui/material/CardContent" props
    static member inline cardContent(children: #seq<ReactElement>) = createElementImportDefault "@mui/material/CardContent" [ cardContent.children (children :> _ seq) ]

    static member inline cardMedia(props) = createElementImportDefault "@mui/material/CardMedia" props
    static member inline cardMedia(children: #seq<ReactElement>) = createElementImportDefault "@mui/material/CardMedia" [ cardMedia.children (children :> _ seq) ]

    static member inline cardHeader(props) = createElementImportDefault "@mui/material/CardHeader" props

    static member inline snackbar(props) = createElementImportDefault "@mui/material/Snackbar" props
    static member inline snackbar(value: ReactElement) = createElementImportDefault "@mui/material/Snackbar" [ snackbar.children value ]

    static member inline pagination(props) = createElementImportDefault "@mui/material/Pagination" props

    static member inline circularProgress(props) = createElementImportDefault "@mui/material/CircularProgress" props

    static member inline select(props) = createElementImportDefault "@mui/material/Select" props

    static member inline menuItem(props) = createElementImportDefault "@mui/material/MenuItem" props

    static member inline checkbox(props) = createElementImportDefault "@mui/material/Checkbox" props

    static member inline link(props) = createElementImportDefault "@mui/material/Link" props

    static member inline modal(props) = createElementImportDefault "@mui/material/Modal" props

[<RequireQualifiedAccess>]
module MuiE =

    let inline boxSx (sx: obj) props =
        Mui.box (box.sx sx :: props)

    let inline stackCol props =
        Mui.stack (stack.direction.column :: props)

    let inline stackRow props =
        Mui.stack (stack.direction.row :: props)
