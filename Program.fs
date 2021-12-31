open System
open Spectre.Console

module StarWars =
    module Caching =
        open Microsoft.Extensions.Caching.Memory
        open Polly

        let build func =
            let cache =
                Policy.Cache(
                    Caching.Memory.MemoryCacheProvider(
                        new MemoryCache(MemoryCacheOptions())
                    ),
                    Caching.RelativeTtl TimeSpan.MaxValue
                )
            fun arg -> cache.Execute((fun _ -> func arg), Context (string arg))

    open FSharp.Data

    type People = JsonProvider<"https://swapi.dev/api/people">
    type Species = JsonProvider<"https://swapi.dev/api/species/1">
    type Planet = JsonProvider<"https://swapi.dev/api/planets/1">

    let getSpecies : string -> _ = Caching.build Species.Load
    let getPlanet : string -> _ = Caching.build Planet.Load

    let getPeople () =
        let rec getPage (people:People.Root) = seq {
            people.Results
            if not (isNull people.Next) then
                yield! getPage (People.Load people.Next)
        }
        People.GetSample() |> getPage

let (|ParsedConsoleColor|_|) (color:string) =
    let capitalised = (Char.ToUpper(color.[0]).ToString()) + color.[1..]
    match ConsoleColor.TryParse capitalised with
    | true, color -> Some (ParsedConsoleColor color)
    | false, _ -> None

let table =
    Table(Caption = TableTitle "Star Wars Characters")
        .Centered()
        .DoubleBorder()

for column in [ "Name"; "Eye Colour"; "Hair Colour"; "Species"; "Homeworld" ] do
    table.AddColumn column |> ignore

for person in StarWars.getPeople() |> Seq.concat |> Seq.take 10 do
    let species =
        match Array.tryHead person.Species with
        | Some s -> StarWars.getSpecies(s).Name
        | None -> "Unknown"
    let homeworld = StarWars.getPlanet person.Homeworld |> fun s -> s.Name    
    let chooseColor color =
        match color with
        | ParsedConsoleColor color -> color
        | "Unknown" | "n/a" | "none" | "unknown" -> ConsoleColor.Gray
        | _ -> ConsoleColor.White
    table
        .AddRow(
            Text person.Name,
            Markup $"[{chooseColor person.EyeColor}]{person.EyeColor}[/]",
            Markup $"[{chooseColor person.HairColor}]{person.HairColor}[/]",
            Markup $"[{chooseColor species}]{species}[/]",
            Markup $"[{chooseColor homeworld}]{homeworld}[/]"
        )
    |> ignore

AnsiConsole.Write table