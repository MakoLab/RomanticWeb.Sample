/**
![logo](http://romanticweb.net/images/logo-small.png)

# Romantic Web Sample

This project is a small app, which demonstrates the use of [Romantic Web](http://romanticweb.net), the RDF mapper for .NET.

What you will read below is actual source code from the [Program.cs](http://github.com/makolab/romanticweb.sample/Program.cs) file.

## Preparations

NuGet packages must first be restored by running `_restore-packages.bat` before opening the solution.

Also here are the required namespaces:
**/

using System;
using System.Collections.Generic;
using System.Linq;
using RomanticWeb;
using RomanticWeb.DotNetRDF;
using RomanticWeb.Entities;
using RomanticWeb.Mapping.Attributes;
using RomanticWeb.Mapping.Fluent;
using RomanticWeb.Ontologies;
using VDS.RDF;
using VDS.RDF.Writing;

/**
## Model and mappings

First, let's create some [model interfaces and mappings][map]. First we'll define a person entity, mapped with [FoaF][foaf] terms using 
mapping attributes. This way we instruct Romantic Web how each property should be serialized to RDF. Note how both absolute and [prefixed
URIs][qname] can be used.
**/

[Class("http://xmlns.com/foaf/0.1/Person")]
public interface IPerson : IEntity
{
    [Property("foaf", "givenName")]
    string Name { get; set; }

    [Property("foaf", "familyName")]
    string LastName { get; set; }

    [Collection("foaf", "publications")]
    ICollection<IPublication> Publications { get; set; }
}

/**
Here's the publication entity mapped with [Dublin Core][dc].
**/

public interface IPublication : IEntity
{
    string Title { get; set; }

    DateTime DatePublished { get; set; }
}

/**
Note that there are no mapping attributes. This time the mappings will be defined in a separate mapping class using API inspired by
[Fluent NHibernate][fnh].
**/

public class PublicationMapping : EntityMap<IPublication>
{
    public PublicationMapping()
    {
        Property(pub => pub.Title).Term.Is("dcterms", "title");
        Property(pub => pub.DatePublished).Term.Is(new Uri("http://purl.org/dc/elements/1.1/date"));
    }
}

/**
Now everyting is set to execute sample code, which will query and modify some data.
**/

class Program
{
    static void Main()
    {
        ITripleStore store = new TripleStore();
        store.LoadFromFile("input.trig");

        var factory = new EntityContextFactory();

        // it is also possible to add fluent/attribute mappings separately
        factory.WithMappings(mb => mb.FromAssemblyOf<IPerson>());

        factory.WithMetaGraphUri(new Uri("http://romanticweb.net/samples"));

        // this API bound to change in the future
        factory.WithOntology(new DefaultOntologiesProvider(BuiltInOntologies.DCTerms | BuiltInOntologies.FOAF));

        factory.WithDotNetRDF(store);

        using (var context = factory.CreateContext())
        {
            foreach (var person in context.AsQueryable<IPerson>().Where(p => p.Name == "Tim"))
            {
                var pub = context.Create<IPublication>("http://romanticweb.net/samples/publication/" + Guid.NewGuid());
                pub.Title = string.Format("Publication about RDF by {0} {1}", person.Name, person.LastName);
                pub.DatePublished = DateTime.Now;

                person.Publications.Add(pub);
            }

            context.Commit();
        }

        store.SaveToFile("output.trig", new TriGWriter());
    }
}

/**
[foaf]: http://xmlns.com/foaf/spec/
[dc]: http://purl.org/dc/terms/
[map]: http://romanticweb.net/docs/basic-usage/mapping/
[qname]: http://en.wikipedia.org/wiki/QName
[fnh]: http://www.fluentnhibernate.org/
**/