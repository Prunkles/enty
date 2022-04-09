namespace enty.Web.App

open enty.Core


module MindApiImpl =

    open enty.Mind.Client.Fable

    let mindApi: IMindApi =
        upcast FetchMindApi("/mind")
