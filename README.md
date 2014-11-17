Dwarf
=====
>Overview Text

##Setup / Getting started
##Examples

##Features
###The Config object
In the initialization code of your application, e.g. Application_Start in global.asax in a web application, construct the DwarfConfiguration object. Then save the reference to this object so it's accessible throughout your code. The config object is your gateway to all bonus features of Dwarf (transactions, ad-hoc queries, database scripts, audit logging, error logging, etc.)


####Global.asax
If you're building web application, let your global.asax inherit from DwarfGlobal and thusly let Dwarf handle configuration tracking, error handling, request/context/cache handling, etc

####Generate scripts
####Ad-Hoc queries
###No duplicate instances!
Dwarf keeps all objects unique, meaning that the same object will be referenced rather than creating an identical object when querying the database. This is accomplished via keeping all objects loaded from the database in the user/request unique first-level-cache.

###Only save changes
Dwarf will only persist dirty properties, thus unmodified properties will not be part of the update script

###Cascading saves & deletes
By default an object's OneToMany and ManyToMany relationships/collections are automatically saved when saving the object. Delete's are primarily handled via delete cascading in the database for performance. Delete cascades can be disabled on a property basis allowing for manual delete handling. Just set DisableDeleteCascades to true on the foreign key's DwarfProperty-attribute.

###Inverse collections
In most cases it's natural that if an object is deleted, its collections should be deleted as well. But sometimes the semantic relationship is the opposite. For example if a person is deleted should all its pets be deleted too? This is accomplished by inversing the relationship via setting the Inverse-property to true on the OneToMany-attribute and setting IsNullable to true on the foreign key property.

###The Dwarf base class
####Properties
####OneToMany
####ManyToMany
####Interfaces
####Save/Delete
####Load/LoadAll/LoadReferencing
###The UnaryDwarf base class
###Goblins & GoblinCollections
###The QueryBuilder
