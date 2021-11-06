open Spectre.Console
open System.Collections.Generic
open System

module StarWars =
    type People = FSharp.Data.JsonProvider<"https://swapi.dev/api/people">
    type Species = FSharp.Data.JsonProvider<"https://swapi.dev/api/species/1">
    type Planet = FSharp.Data.JsonProvider<"https://swapi.dev/api/planets/1">

    let cache f =
        let d = Dictionary()
        fun x ->
            if not (d.ContainsKey x) then d.[x] <- f x
            d.[x]

    let getSpecies : string -> _ = cache Species.Load
    let getPlanet : string -> _ = cache Planet.Load
    let getPeople () =
        let rec getPage (people:People.Root) = seq {
            people.Results
            if not (isNull people.Next) then
                yield! getPage (People.Load people.Next)
        }
        People.GetSample() |> getPage

let table =
    Table(Caption = TableTitle "Star Wars Characters")
        .Centered()
        .DoubleBorder()

[ "Name"; "Eye Colour" ;"Hair Colour" ;"Species" ;"Homeworld" ]
|> List.iter (table.AddColumn >> ignore)

type System.String with
    member this.Capitalise () = (Char.ToUpper(this.[0]).ToString()) + this.[1..]

for person in StarWars.getPeople() |> Seq.concat |> Seq.take 10 do
    let greyOrWhite = function
        | "Unknown" | "n/a" | "none" | "unknown" -> ConsoleColor.Gray
        | _ -> ConsoleColor.White
    let species =
        match Array.tryHead person.Species with
        | Some s -> StarWars.getSpecies(s).Name
        | None -> "Unknown"
    let homeworld = StarWars.getPlanet person.Homeworld |> fun s -> s.Name
    let validateColor (color:string) =
        match ConsoleColor.TryParse (color.Capitalise ()) with
        | true, color -> color
        | false, _ -> greyOrWhite color
    table
        .AddRow(
            Text(person.Name),
            Markup($"[{validateColor person.EyeColor}]{person.EyeColor}[/]"),
            Markup($"[{validateColor person.HairColor}]{person.HairColor}[/]"),
            Markup($"[{greyOrWhite species}]{species}[/]"),
            Markup($"[{greyOrWhite homeworld}]{homeworld}[/]")
        )
    |> ignore

AnsiConsole.Write table