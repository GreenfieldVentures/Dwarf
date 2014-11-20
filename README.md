Dwarf
=====
Dwarf.Net is a light weight, highly competent, versatile, easy-to-use O/R-M framework. Since the project was initiated in 2008 its goal has always been to minimize boiler plate code while maintaining high performance and readability. Dwarf aims to never "get in your way" which makes it an excellent companion during all stages of your project! Dwarf is currently used in a multitude of applications spanning from single-user desktop apps to online api backends with thousands of users. 

##Minimal example
```csharp
public class Program
{
    static void Main(string[] args)
    {
        var cfg = new DwarfConfiguration<Program> { ConnectionString ="..." }.Configure();
        cfg.DatabaseScripts.ExecuteCreateScript();
        new Person { Name = "Carl" }.Save();
    }
}

public class Person : Dwarf<Person>
{
    [DwarfProperty]
    public string Name { get; set; }
}
```

##FAQ
#####Does Dwarf use reflection?
No. Reflection is slow, therefore Dwarf instead relies heavily on compiled expressions to access all properties and methods.

#####Does Dwarf support Lazy Loading?
Yes. All relationships/collections implement Lazy Loading by design. Foreign key properties supports both lazy loading and eager loading. Either by decorating the property as virtual to let the proxy generator handle the lazy loading or set the EagerLoad property to true in the dwarf attribute.

#####Which databases does Dwarf support?
Dwarf has full support for Sql Server and Sql Ce. Its internals are highly extendable thus adding support for additional providers is fairly easy, though not yet requested.

#####Configuration and Conventions?
Dwarf is a wee bit opinionated and is built upon its own set of conventions, were some are overridable and some aren't. There are no mapping files, instead mapping is done via property attributes. Less files, less code and less scattered code without making the domain model unreadable.

#####Linq?
Over the years so many good developers have blown their poor feet off due to lack of understanding of abstractions made by frameworks at hand. We've decided never to create a linq provider for Dwarf mainly to make a clear separation between querying the database and querying collections. It takes some intellection to know what parts of a more complex query that is suitable for a database and what parts that are not. Thus we use the QueryBuilder (see below) to offer an easy way of constructing database queries and to make the distinction of what code is executed where more clear.

##Features
###The Config object
In the initialization code of your application, e.g. Application_Start in global.asax in a web application, construct the DwarfConfiguration object. Then save the reference to this object so it's accessible throughout your code. The config object is your gateway to all bonus features of Dwarf (transactions, ad-hoc queries, database scripts, audit logging, error logging, etc.)

######Basic Example
```csharp
var cfg = new DwarfConfiguration<Person> { ConnectionString = "MyConnectionString" }.Configure();
```

######Basic Example with built-in audit logging and error logging
```csharp
var cfg = new DwarfConfiguration<Person>
{
    UseDefaultAuditLogService = true, /* Optional. You may provide your own service too */
    UseDefaultErrorLogService = true, /* Optional. You may provide your own service too */      
    ConnectionString = "MyConnectionString",
}.Configure();
```

The type Person can be any arbitrary type residing in the same assembly as your domain model. Preferably use a type signifying your model.

####Global.asax
If you're building web application, let your global.asax inherit from DwarfGlobal and thusly let Dwarf handle configuration tracking, error handling, request/context/cache handling, etc

####Generate scripts
```csharp
var createScript = cfg.DatabaseScripts.GetCreateScript();    

var dropScript = cfg.DatabaseScripts.GetDropScript();    

//Prepares a script for transfering data from another database. Optional: specify which properties to skip
var transferScript = cfg.DatabaseScripts.GetTransferScript("MyOtherDatabase"); 

//Analyses and compares the model with the database and generates the upgrade necessary scripts
var updateScript = cfg.DatabaseScripts.GetUpdateScript(); 

//Executes drop scripts
cfg.DatabaseScripts.ExecuteDropScript(); 

//Executes drop scripts then create scripts
cfg.DatabaseScripts.ExecuteCreateScript(); 
```

####Ad-Hoc queries
```csharp
//Execute ad-hoc queries that returns dynamic objects
var result = cfg.Database.ExecuteQuery("select count(*) as Name, name from disease group by name");

foreach (var o in result)
    Console.WriteLine(o.Name + " " + o.name);

//Execute ad-hoc queries.
cfg.Database.ExecuteNonQuery("create view Temp as select Id from Person where Age > 15");

//Execute ad-hoc scalar queries.
cfg.Database.ExecuteScalar<int?>("select top 1 age from Person");
```

####Suspend & Resume audit logging
```csharp
//Suspend and Resume auditlogging for the current session
cfg.SuspendAuditLogging();
cfg.ResumeAuditLogging();
```

####Transactions
Every database operation (apart from reading) is wrapped inside a transaction. You manually can extend the transaction scope to span over multiple operations
```csharp
//Transcation.Commit will be called if no exception is thrown, 
//otherwise Transaction.Rollback will be called (unless exception handled manually)
using (cfg.Database.OpenTransaction())
{
    new Pet {Name = "Billy the Cat"}.Save();
    new Disease {Name = "Pink Eye"}.Save();
}

//Manually rollback the transcation
using (var transaction = cfg.Database.OpenTransaction())
{
    new Pet {Name = "Billy the Cat"}.Save();
    new Disease {Name = "Pink Eye"}.Save();

    //Explicit rollback
    transaction.Rollback();
}
```

####Error logging
```csharp
//Manual error logging. Internally generated exceptions are handled automatically
cfg.ErrorLogService.Logg(new Exception("My application just crashed", new Exception("This is an inner exception")));
```

####Audit Logging
```csharp
var myCat = new Pet { Name = "Whiskers" };
myCat.Save(); 

myCat.Name = "Whiskers III";
myCat.Save();

var auditLogs = cfg.AuditLogService.LoadAllReferencing(myCat);

foreach (var auditLog in auditLogs)
{
    var names = auditLog.GetLogData<Pet, string>(x => x.Name);

    if (names != null)
        Console.WriteLine(names.OldValue + " was renamed " + names.NewValue + " on " auditLog.TimeStamp);
}
```

###No duplicate instances!
Dwarf keeps all objects unique, meaning that the same object will be referenced rather than creating an identical object when querying the database. This is accomplished via keeping all objects loaded from the database in the user/request unique first-level-cache.

###Only save changes
Dwarf will only persist dirty properties, thus unmodified properties will not be part of the update script

###Cascading saves & deletes
By default an object's OneToMany and ManyToMany relationships/collections are automatically saved when saving the object. Delete's are primarily handled via delete cascading in the database for performance. Delete cascades can be disabled on a property basis allowing for manual delete handling. Just set DisableDeleteCascades to true on the foreign key's DwarfProperty-attribute.

###Inverse collections
In most cases it's natural that if an object is deleted, its collections should be deleted as well. But sometimes the semantic relationship is the opposite. For example if a person is deleted should all its pets be deleted too? This is accomplished by inversing the relationship via setting the Inverse-property to true on the OneToMany-attribute and setting IsNullable to true on the foreign key property.

###The Dwarf base class
Creating a class
```csharp
public class Person : Dwarf<Person>
{
}
```

####Properties
Here are some examples of properties of different types and DwarfPropertyAttribute settings. Dwarf supports DateTime, Enum, int, decimal, double, bool, Type, byte[], IDwarf (foreign keys), IGem & IGemLists
```csharp
[DwarfProperty]
public string Name { get; set; }

[DwarfProperty]
public int Age { get; set; }

[DwarfProperty]
public double? BeardSize { get; set; }

[DwarfProperty(UseMaxLength = true)]
public string History { get; set; }

[DwarfProperty]
public virtual Mountain Home { get; set; } //Virtual to enable Lazy Loading

[DwarfProperty(IsUnique = true)]
public string SocialSecurityNumber { get; set; }

[DwarfProperty(DisableDeleteCascade = true, IsNullable = true)]
public virtual Country Country { get; set; }

[DwarfProperty(UniqueGroupName = "UniqueGroupWithCoolName")]
public string PropertyOne { get; set; }

[DwarfProperty(UniqueGroupName = "UniqueGroupWithCoolName")]
public string PropertyTwo { get; set; }
```
Notice that the IsNullable property on the attributes does not have to be set for nullable value types (i.e. int? double?, DateTime?, etc).

####ProjectionProperties
Projection properties behaves like readonly properies where the data is fetched by a custom query. These properties are queryable like and other property (linq or the QueryBuilder). They serve as a means to avoid unnecessary round trips to the database
```csharp
[DwarfProjectionProperty("select some_column from another_table at where at.personId = person.id")]
public bool IsSomethingValid { get; set; }
```

####OneToMany & ManyToMany collections
Dwarf keeps track of all collection's added/removed/updated objects and will take care of the necessary database operations. 
Many-to-many collections are also mapped and handled automatically.

You can either let Dwarf handle the naming of the Many-To-Many tables or manually assign one
```csharp
[ManyToMany]
public DwarfList<Disease> Diseases
{
    get { return ManyToMany(x => x.Diseases); }
}

[ManyToMany(TableName = "CurrentlyActiveDiseases")]
public DwarfList<Disease> Diseases
{
    get { return ManyToMany(x => x.Diseases); }
}
```

A regular OneToMany property
```csharp
[OneToMany]
public DwarfList<Memory> Memories
{
    get { return OneToMany(x => x.Memories); }
}
```

A OneToMany property where the foreign key in the other type is named other than the calling type. I.e. if the implementing type is Person, then Memory's foreign key must be named Person for Dwarf to automatically handle the relationship. Otherwise the foreign key must be specified as:
```csharp
[OneToMany]
public DwarfList<Memory> Memories
{
    get { return OneToMany(x => x.Memories, null, x => x.TheOwningProperty); }
}
```

As described above regarding inverse relationships
```csharp
[OneToMany(IsInverse = true)]
public DwarfList<Memory> Memories
{
    get { return OneToMany(x => x.Memories); }
}
```

By default the DwarfLists will use the containing object's id (or composite id) as a primary key, but an alternate key can be specified to be used when determining if an object should be added or not to the collection
```csharp
[OneToMany]
public DwarfList<BirthdayParty> Birthdays
{
    get { return OneToMany(x => Birthdays, x => x.Ordinal); }
}
```

When adding objects to the collection you don't need to assign the foreign key. This is done automatically during save. An example:
```csharp
myPerson.Pets.Add(new Pet { Name = "Snoopy" });
myPerson.save(); //during this step the foreign key property pet.Person will be set to myPerson

```

####Extension points
There are two extension points each for the save and delete operations which can be overridden
* PrependSave
* AppendSave
* PrependDelete
* AppendDelete
They all occur inside an ongoing transaction but prior to or after the command is sent to the database. 


####Interfaces
Assign behavior to objects in the model by decorating them with interfaces
* ICacheless - Objects of this type will not be subject to caching
* ICompositeId - Discard the default Id-property and instead compose a primary key from all DwarfProperties with IsPrimaryKey = true
* ITranscationless - Objects of this type will always be handled outside of and will not be affected by any ongoing transcation

####Other Functions

```csharp
//Revert all local changes to the object's properties
home.Reset();

//Reload all property values for object from the database
home.Refresh();

//Clone an object
var clone = home.CloneX();

//True if any properties on the object have changed
var isDirty = home.IsDirty;

//True if the name property has changed
var isPropertyDirty = home.IsPropertyDirty(x => x.Location);

//True if the object has been saved
var isSaved = home.IsSaved;
```

####Save/Delete
```csharp
//Save an object
new Mountain {Location = "Himalaya"}.Save();

//or
var home = new Mountain {Location = "Himalaya"}.SaveX();

//Update an object
home.Location = "Erebor";
home.Save();

//Delete an object
home.Delete();

//You may call SaveAll or DeleteAll on DwarfLists 
new DwarfList<Mountain>
{
    new Mountain {Location = "Himalaya"},
    new Mountain {Location = "Erebor"},
}.SaveAll();

```

####Load & LoadAll

```csharp
//Load an object from the database 
var pet = Pet.Load(myPetId);
pet.Name = "Garfield";

//Load all pets from the database
var allPets = Pet.LoadAll();
```

####Bulk insert
Sometimes you need to save a huge amount of data and what better way to do that than via BulkInsert?
From inside any Dwarf object you can call the protected Method BulkInsert and pass it a list of objects. 
Example
```csharp
public class Pet : Dwarf<Pet>
{
    [DwarfProperty]
    public string Name { get; set; }

    public static void MegaSave(IEnumerable<Pet> pets)
    {
        BulkInsert(pets);
    }
}
```` 

###The UnaryDwarf base class
Your "tree structure"-like types can inherit from UnaryDwarf instead of Dwarf. 
This will add two properties to the type:
* Parent
* Children

And the following utility functions:
* LoadAncestors
* LoadDescendants
* LoadRoots

###Gems
A gem is a value object, meaning an object not persisted by the current database like a type inheriting from dwarf. I.e. it can be a wrapper for a service or any other composed datatype. Dwarf objects will save the Id property as a reference. Here's a simple examble of a Gem type called MagicNumber
```csharp
public class MagicNumber : Gem<MagicNumber>
{
    public override object Id
    {
        get { return TheSecretSauce; }
    }

    public int TheSecretSauce { get; set; }

    public override MagicNumber LoadImplementation(object id)
    {
        //Imagine a call to a REST service
        return new MagicNumber { TheSecretSauce = (int)id };
    }

    public override List<MagicNumber> LoadAllImplementation()
    {
        //Imagine a call to a REST service
        return new List<MagicNumber>
        {
            new MagicNumber{ TheSecretSauce = 1 },
            new MagicNumber{ TheSecretSauce = 2 },
            new MagicNumber{ TheSecretSauce = 3 }
        }; 
    }
}
```

To implement MagicNumber in or Dwarf class we can either chose to reference one object:
```csharp
[DwarfProperty]
public MagicNumber MyLuckyNumber { get; set; }
```

Or a collection of Gems
```csharp
public GemList<MagicNumber> MagicNumbers
{
    get { return Gems(x => x.MagicNumbers); }
}
```
Note that GemLists don't need an attribute to be handled


###The QueryBuilder
The query builder supports 
* Nested queries
* Inner and Left Outer Joins
* Almost any Where-clause (are constructed with the WhereCondition-objects
* "Where"s with inner or-clauses
* Ordering
* Grouping
* Distinct queries
* Can construct update & delete queries (though they ought to be rarely used)
* All through a fluent interface. 

The QueryBuilder is your friend! Type-safety above all and let the compiler tell you when a change in the model will break a query, not at runtime! Don't rely on "search & replace" when updating your code.

Some examples
```csharp
public static List<Pet> LoadAllPetsNamed(string name)
{
    var query = new QueryBuilder()
        .Select<Pet>()
        .From<Pet>()
        .Where<Pet>(x => x.Name, name);

    return LoadReferencing<Pet>(query);
} 

public static List<Person> LoadAllPeopleWithPetsNamed(string name)
{
    var query = new QueryBuilder()
        .Select<Person>()
        .From<Person>()
        .InnerJoin<Pet, Person>(x => x.Owner, x => x)
        .Where<Pet>(x => x.Name, name)
        .OrderBy<Person>(x => x.Age);

    return LoadReferencing<Person>(query);
} 

public static List<Person> SomeWierdNonsenseQuery()
{
    var query = new QueryBuilder()
        .Select<Pet>()
        .From<Pet>()
        .LeftOuterJoin<Person, Pet>(x => x, x => x.Owner)
        .Where<Person>(x => x.Age, QueryOperators.LessThan, 50)
        .Where<Person>(x => x.Name, QueryOperators.Like, "Hans")
        .Where<Person>(x => x.BeardSize, 15)
        .Where<Person>(DateParts.Year, x => x.BirthDay, QueryOperators.In, new List<int> { 1985, 1987, 1989 })
        .Where<Person>(x => x.MyLuckyNumbers, QueryOperators.Contains, MagicNumber.Load(53))
        .WhereWithInnerOrClause
        (
            new WhereCondition<Pet>(x => x.Name, QueryOperators.IsNot, null),
            new WhereCondition<Pet>(x => x.Name, QueryOperators.Like, "uste")
        )
        .OrderBy<Pet>(x => x.Pet);

    return LoadReferencing<Person>(query);
} 
```
