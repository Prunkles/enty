module enty.Mind.Server.LinqToDbPostgresExtensions

open LinqToDB

[<RequireQualifiedAccess>]
module Sql =
    type Json =
        /// {0}->{1}
        [<Sql.Expression("{0}->{1}", ServerSideOnly=true, InlineParameters=true)>]
        static member Value(field: _, propName: string) = invalidOp ""
        /// {0}->>{1}
        [<Sql.Expression("{0}->>{1}", ServerSideOnly=true, InlineParameters=true)>]
        static member ValueText(field: _, propName: string): string = invalidOp ""
        /// {0}#>{1}
        [<Sql.Expression("{0}#>{1}", ServerSideOnly=true, InlineParameters=true)>]
        static member Path(field: _, path: string[]) = invalidOp ""
        /// {0}#>>{1}
        [<Sql.Expression("{0}#>>{1}", ServerSideOnly=true, InlineParameters=true)>]
        static member PathText(field: _, path: string[]): string = invalidOp ""
        /// to_json({0})
        [<Sql.Expression("to_json({0})", ServerSideOnly=true, InlineParameters=true)>]
        static member ToJson(anyElement: _) = invalidOp ""
        /// {0}::json
        [<Sql.Expression("{0}::json", ServerSideOnly=true, InlineParameters=true)>]
        static member AsJson(json: string) = invalidOp ""
        /// {0}::jsonb
        [<Sql.Expression("{0}::jsonb", ServerSideOnly=true, InlineParameters=true)>]
        static member AsJsonb(json: string) = invalidOp ""
        /// {0} ? {1}
        [<Sql.Expression("{0} ? {1}", ServerSideOnly=true, InlineParameters=true)>]
        static member Contains(json: _, element: string) = invalidOp ""
        
    
    type Jsonb =

        [<Sql.Expression("jsonb_set({0}, {1}, {2})", ServerSideOnly=true, InlineParameters=true)>]
        static member JsonbSet(field: _, path: string, newValue: string) = invalidOp ""

type Sql =
    [<LinqToDB.Sql.Expression("{0}::text", ServerSideOnly=true, InlineParameters=true)>]
    static member AsText(value: _): string = invalidOp ""
