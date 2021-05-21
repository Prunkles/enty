module enty.WebApp.SenseInputing

open Feliz
open Feliz.MaterialUI

[<ReactComponent>]
let SenseInput () =
    let input, setInput = React.useState("")
    Mui.textareaAutosize [
        
    ]