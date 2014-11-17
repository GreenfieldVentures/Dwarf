Dwarf
=====
Overview Text

##Setup / Getting started
##Examples

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
Dwarf keeps track of all collection's added/removed/updated objects and will take care of the necessary database operations. 
Many-to-many collections are also mapped and handled automatically.

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

####Properties
####OneToMany
####ManyToMany
####Interfaces
####Save/Delete
####Load/LoadAll/LoadReferencing
```csharp
//Load an object from the database 
var pet = Pet.Load(myPetId);
pet.Name = "Garfield";

//Load all pets from the database
var allPets = Pet.LoadAll();
```

###The UnaryDwarf base class
###Goblins & GoblinCollections
###The QueryBuilder
