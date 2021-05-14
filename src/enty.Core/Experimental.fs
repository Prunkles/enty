module enty.Core.Experimental

type Sense =
    | Value of string
    | List of Sense list
