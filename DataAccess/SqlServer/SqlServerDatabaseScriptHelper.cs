using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text;
using Evergreen.Dwarf.Attributes;
using Evergreen.Dwarf.Extensions;
using Evergreen.Dwarf.Interfaces;
using Evergreen.Dwarf.Utilities;
using System.Dynamic;

namespace Evergreen.Dwarf.DataAccess
{
    /// <summary>
    /// Utility class for generating sql scripts from the current domain model
    /// </summary>
    internal static class SqlServerDatabaseScriptHelper
    {
        #region GetCreateScript

        /// <summary>
        /// Converts and returns the current domain model as create scripts (incl keys)
        /// </summary>
        internal static string GetCreateScript<T>(string connectionString = null, bool hideOptionalStuff = false)
        {
            var tables = new StringBuilder();
            var constraints = new StringBuilder();
            var manyToManyTables = new StringBuilder();

            foreach (var type in DwarfHelper.GetValidTypes<T>())
                CreateTable<T>(tables, type, constraints, manyToManyTables);

            return tables.ToString() + manyToManyTables + constraints;
        }

        #endregion GetCreateScript

        #region GetDropScript

        /// <summary>
        /// Converts and returns the current domain model as a drop script
        /// </summary>
        internal static string GetDropScript<T>()
        {
            return GetDropConstraintsScript<T>() + GetDropTablesScript<T>();
        }

        private static string GetDropTablesScript<T>()
        {
            var tables = new StringBuilder();
            var manyToManyTables = new StringBuilder();

            foreach (var type in DwarfHelper.GetValidTypes<T>())
            {
                foreach (var manyToManyProperty in DwarfHelper.GetManyToManyProperties(type))
                {
                    var tableName = ManyToManyAttribute.GetTableName(type, manyToManyProperty.ContainedProperty);

                    if (!manyToManyTables.ToString().Contains("DROP TABLE [" + tableName + "]"))
                        manyToManyTables.AppendLine("IF EXISTS (SELECT * FROM dbo.sysobjects WHERE Id = OBJECT_ID(N'[" + tableName + "]') AND OBJECTPROPERTY(Id, N'IsUserTable') = 1) DROP Table [" + tableName + "]");
                }
                if (type.Implements<IDwarf>() && !type.IsAbstract)
                    tables.AppendLine("IF EXISTS (SELECT * FROM dbo.sysobjects WHERE Id = OBJECT_ID(N'[" + type.Name + "]') AND OBJECTPROPERTY(Id, N'IsUserTable') = 1) DROP Table [" + type.Name + "]");
            }

            return manyToManyTables.ToString() + tables;
        }

        private static string GetDropConstraintsScript<T>()
        {
            var constraints = new StringBuilder();

            foreach (var type in DwarfHelper.GetValidTypes<T>())
            {
                foreach (var pi in DwarfHelper.GetFKProperties<T>(type))
                {
                    var fk = "FK_" + type.Name + "_" + pi.Name + "Id";
                    constraints.AppendLine("IF EXISTS (SELECT 1 FROM sys.objects WHERE OBJECT_ID = OBJECT_ID(N'[" + fk + "]') AND PARENT_OBJECT_ID = OBJECT_ID('[" + type.Name + "]')) ALTER TABLE [" + type.Name + "] DROP CONSTRAINT [" + fk + "]");
                }
            }

            return constraints.ToString();
        }


        #endregion GetDropScript

        #region ExecuteDropScript

        /// <summary>
        /// Executes scripts for dropping all tables in the database
        /// </summary>
        internal static void ExecuteDropScript<T>()
        {
            var constraints = GetDropConstraintsScript<T>();

            if (!string.IsNullOrEmpty(constraints))
                DwarfContext<T>.GetDatabase().ExecuteNonQuery<T>(constraints);

            DwarfContext<T>.GetDatabase().ExecuteNonQuery<T>(GetDropTablesScript<T>());
        }

        #endregion ExecuteDropScript

        #region GetDeleteCurrentDatabaseScript

        /// <summary>
        /// Executes scripts for dropping all tables in the database
        /// </summary>
        private static string GetDeleteCurrentDatabaseScript<T>()
        {
            var sb = new StringBuilder();

            var constraints = DwarfContext<T>.GetDatabase().ExecuteCustomQuery<T>("SELECT OBJECT_NAME(parent_object_id) AS TableName, OBJECT_NAME(OBJECT_ID) AS NameofConstraint FROM sys.objects WHERE type_desc LIKE '%CONSTRAINT' ORDER BY Type");

            foreach (var constraint in constraints)
                sb.AppendLine("ALTER TABLE [" + constraint.TableName + "] DROP CONSTRAINT [" + constraint.NameofConstraint + "] ");

            var tables = DwarfContext<T>.GetDatabase().ExecuteCustomQuery<T>("SELECT OBJECT_NAME(OBJECT_ID) AS NameofConstraint FROM sys.objects WHERE type_desc LIKE 'USER_TABLE' ");

            foreach (var table in tables)
                sb.AppendLine("DROP TABLE [" + table.NameofConstraint + "] ");

            return sb.ToString();
        }

        #endregion GetDeleteCurrentDatabaseScript

        #region ExecuteCreateScript

        /// <summary>
        /// Executes scripts for deleting the current and creating the new database
        /// </summary>
        internal static void ExecuteCreateScript<T>()
        {
            var deleteCurrentDatabaseScript = GetDeleteCurrentDatabaseScript<T>();

            if (!string.IsNullOrEmpty(deleteCurrentDatabaseScript))
                DwarfContext<T>.GetDatabase().ExecuteNonQuery<T>(deleteCurrentDatabaseScript);

            DwarfContext<T>.GetDatabase().ExecuteNonQuery<T>(GetCreateScript<T>(Cfg.ConnectionString[typeof(T).Assembly], true));
        }

        #endregion ExecuteCreateScript

        #region GetTransferScript

        /// <summary>
        /// Returns the current domain model as "insert into" from another database with the same structure. 
        /// The tables will be unordered (add your constraints afterwards) and remember to tweak each table as needed
        /// </summary>
        internal static string GetTransferScript<T>(string otherDatabaseName, params ExpressionProperty[] toSkip)
        {
            var tables = new StringBuilder();
            var manyToManyTables = new StringBuilder();

            foreach (var type in DwarfHelper.GetValidTypes<T>())
            {
                var typeProps = (DwarfHelper.GetDBProperties(type).Union(DwarfHelper.GetGemListProperties(type))).Where(x => !toSkip.Contains(x)).Flatten(x => "[" + (x.PropertyType.Implements<IDwarf>() ? x.Name + "Id" : x.Name) + "], ");
                typeProps = typeProps.TruncateEnd(2);

                tables.AppendLine(string.Format("INSERT INTO [{0}] ({1}) (SELECT {1} from [{2}].dbo.[{0}])", type.Name, typeProps, otherDatabaseName));

                foreach (var ep in DwarfHelper.GetManyToManyProperties(type))
                {
                    var tableName = ManyToManyAttribute.GetTableName(type, ep.ContainedProperty);

                    if (manyToManyTables.ToString().Contains(tableName))
                        continue;

                    var m2mProps = "[" + type.Name + "Id], [" + ep.PropertyType.GetGenericArguments()[0].Name + "Id]";

                    manyToManyTables.AppendLine(string.Format("INSERT INTO [{0}] ({1}) (SELECT {1} from [{2}].dbo.[{0}])", tableName, m2mProps, otherDatabaseName));
                }
            }

            return tables.ToString() + manyToManyTables;
        }

        #endregion GetTransferScript

        #region GetUpdateScript

        /// <summary>
        /// Analyses the current domain model and database and generates update scripts in an attempt to synchronize them.
        /// Note that a few places might need manual coding, such as when columns become not nullable, when target types changes, etc. Look for "Warning!!"
        /// in the generated code
        /// </summary>
        internal static string GetUpdateScript<T>(Assembly assembly = null)
        {
            var database = Cfg.Databases[assembly ?? typeof(T).Assembly];

            var dropPKConstraints = new StringBuilder();
            var dropFKConstraints = new StringBuilder();
            var dropColumns = new StringBuilder();
            var dropTables = new StringBuilder();
            var addTables = new StringBuilder();
            var addManyToManyTables = new StringBuilder();
            var addColumns = new StringBuilder();
            var addConstraints = new StringBuilder();

            var allCurrentDomainTypes = DwarfHelper.GetValidTypes<T>().ToList();

            var currentManyToManyTables = GetCurrentManyToManyTables(allCurrentDomainTypes).Distinct().ToList();

            var existingDatabaseTables = DwarfContext<T>.GetConfiguration().Database.ExecuteQuery("SELECT t.name FROM sys.tables t JOIN sys.schemas s ON s.schema_id = t.schema_id ").Select(x => x.name).ToList();

            foreach (var existingTable in existingDatabaseTables.ToArray())
            {
                if (!allCurrentDomainTypes.Select(x => x.Name).Any(x => x.Equals(existingTable)) && !currentManyToManyTables.Any(x => x.Equals(existingTable)))
                {
                    DropDeadTableConstraints<T>(dropPKConstraints, dropFKConstraints, existingTable);
                    dropTables.AppendLine("IF EXISTS (SELECT * FROM dbo.sysobjects WHERE Id = OBJECT_ID(N'[" + existingTable + "]') AND OBJECTPROPERTY(Id, N'IsUserTable') = 1) DROP Table [" + existingTable + "] ");
                    existingDatabaseTables.Remove(existingTable);

                    var constraints = DwarfContext<T>.GetDatabase().ExecuteCustomQuery<T>("select CONSTRAINT_NAME from INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE where TABLE_NAME = '" + existingTable + "' ");

                    foreach (var constraint in constraints)
                        AddDropConstraint(dropPKConstraints, dropFKConstraints, "IF EXISTS (SELECT 1 FROM sys.objects WHERE OBJECT_ID = OBJECT_ID(N'[" + constraint.CONSTRAINT_NAME + "]') AND PARENT_OBJECT_ID = OBJECT_ID('[" + existingTable + "]')) ALTER TABLE [" + existingTable + "] DROP CONSTRAINT [" + constraint.CONSTRAINT_NAME + "]");
                }
            }

            foreach (var type in allCurrentDomainTypes)
            {
                if (!existingDatabaseTables.Contains(type.Name))
                    CreateTable<T>(addTables, type, addConstraints, addManyToManyTables);

                foreach (var pi in DwarfHelper.GetManyToManyProperties(type))
                {
                    if (!existingDatabaseTables.Contains(ManyToManyAttribute.GetTableName(type, pi.ContainedProperty)))
                        CreateManyToManyTable(type, pi, addManyToManyTables, addConstraints);
                }

            }

            foreach (var existingTable in existingDatabaseTables)
            {
                if (!allCurrentDomainTypes.Any(x => x.Name.Equals(existingTable)))
                    continue;

                var existingColumns = (List<dynamic>)DwarfContext<T>.GetDatabase().ExecuteCustomQuery<T>("SELECT c.name as name1, t.name as name2, c.max_length, c.is_nullable FROM sys.columns c inner join sys.types t on t.user_type_id = c.user_type_id WHERE object_id = OBJECT_ID('dbo." + existingTable + "') ");
                
                var type = allCurrentDomainTypes.First(x => x.Name.Equals(existingTable));
                var props = DwarfHelper.GetGemListProperties(type).Union(DwarfHelper.GetDBProperties(type)).ToList();

                foreach (var existingColumn in existingColumns)
                {
                    string columnName = existingColumn.name1.ToString();

                    var pi = props.FirstOrDefault(x => x.Name.Equals(existingColumn.name2.ToString().Equals("uniqueidentifier") && !x.Name.Equals("Id") ? (columnName.EndsWith("Id") ? columnName.TruncateEnd(2) : columnName) : columnName));

                    if (pi == null)
                    {
                        dropColumns.AppendLine("IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[" + existingTable + "]') AND Name = '" + columnName + "') ALTER TABLE dbo.[" + existingTable + "] DROP COLUMN " + columnName);

                        if (existingColumn.name2.Equals("uniqueidentifier"))
                        {
                            var fkName = "FK_" + existingTable + "_" + columnName;
                            AddDropConstraint(dropPKConstraints, dropFKConstraints, "IF EXISTS (SELECT 1 FROM sys.objects WHERE OBJECT_ID = OBJECT_ID(N'[" + fkName + "]') AND PARENT_OBJECT_ID = OBJECT_ID('[" + existingTable + "]')) ALTER TABLE [" + existingTable + "] DROP CONSTRAINT [" + fkName + "]");
                        }
                    }
                }

                foreach (var pi in props)
                {
                    if (DwarfPropertyAttribute.GetAttribute(pi.ContainedProperty) == null && !pi.PropertyType.Implements<IGemList>())
                        continue;

                    var existingColumn = existingColumns.FirstOrDefault(x => (pi.PropertyType.Implements<IDwarf>() ? pi.Name + "Id" : pi.Name).Equals(x.name1));

                    if (existingColumn != null)
                    {
                        var typeChanged = !existingColumn.name2.Equals(TypeToColumnType(pi.ContainedProperty));
                        var lengthChanged = pi.PropertyType == typeof(string) && (existingColumn.name2.ToString().Equals(DwarfPropertyAttribute.GetAttribute(pi.ContainedProperty).UseMaxLength ? "-1" : "255"));
                        var nullableChanged = (bool.Parse(existingColumn.is_nullable.ToString()) != IsColumnNullable(type, pi.ContainedProperty));

                        if (typeChanged | nullableChanged | lengthChanged)
                        {
                            addColumns.AppendLine("-- WARNING! TYPE CONVERSION MIGHT FAIL!!! ");
                            addColumns.AppendLine("ALTER TABLE [" + type.Name + "] ALTER COLUMN " + TypeToColumnName(pi) + " " + TypeToColumnConstruction(type, pi.ContainedProperty, true).TruncateEnd(2));
                            addColumns.AppendLine("GO ");
                        }

                        if (pi.PropertyType.Implements<IDwarf>())
                        {
                            var fkName = "FK_" + type.Name + "_" + pi.Name + "Id";
                            var constraintExists = DwarfContext<T>.GetDatabase().ExecuteCustomQuery<T>("SELECT t.name FROM sys.objects obj inner join sys.foreign_key_columns fk on obj.object_id = fk.constraint_object_id inner join sys.columns c on fk.referenced_object_id = c.object_id and fk.referenced_column_id = c.column_id inner join sys.tables t on t.object_id = c.object_id inner join sys.tables t2 on fk.parent_object_id = t2.object_id WHERE obj.type = 'F' and t2.name = '" + type.Name + "' and obj.name = '" + fkName + "' and t.name = '" + pi.PropertyType.Name + "'");

                            if (!constraintExists.Any())
                            {
                                dropColumns.AppendLine("IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[" + existingTable + "]') AND Name = '" + existingColumn.name1 + "') ALTER TABLE dbo.[" + existingTable + "] DROP COLUMN " + existingColumn.name1);

                                AddDropConstraint(dropPKConstraints, dropFKConstraints, "IF EXISTS (SELECT 1 FROM sys.objects WHERE OBJECT_ID = OBJECT_ID(N'[" + fkName + "]') AND PARENT_OBJECT_ID = OBJECT_ID('[" + existingTable + "]')) ALTER TABLE [" + existingTable + "] DROP CONSTRAINT [" + fkName + "]");

                                if (IsColumnNullable(type, pi.ContainedProperty))
                                {
                                    addColumns.AppendLine("ALTER TABLE [" + type.Name + "] ADD " + TypeToColumnName(pi) + " " + TypeToColumnConstruction(type, pi.ContainedProperty).TruncateEnd(2));
                                    addColumns.AppendLine("GO ");
                                }
                                else
                                {
                                    object value = null;

                                    try { value = Activator.CreateInstance(pi.PropertyType); }
                                    catch { }

                                    addColumns.AppendLine("GO ");
                                    addColumns.AppendLine("ALTER TABLE [" + type.Name + "] ADD " + TypeToColumnName(pi) + " " + TypeToColumnType(pi.ContainedProperty));
                                    addColumns.AppendLine("GO ");
                                    addColumns.AppendLine("-- WARNING! Value is probably wrong. Correct before you execute! ");
                                    addColumns.AppendLine("UPDATE [" + type.Name + "] SET " + TypeToColumnName(pi) + " = " + database.ValueToSqlString(value) + " ");
                                    addColumns.AppendLine("GO ");
                                    addColumns.AppendLine("ALTER TABLE [" + type.Name + "] ALTER COLUMN " + TypeToColumnName(pi) + " " + TypeToColumnConstruction(type, pi.ContainedProperty, true).TruncateEnd(2));
                                    addColumns.AppendLine("GO ");

                                }

                                var alterTable = "ALTER TABLE [" + type.Name + "] ADD CONSTRAINT [" + fkName + "] FOREIGN KEY (" + pi.Name + "Id) REFERENCES [" + pi.PropertyType.Name + "] (Id)";

                                if (!DwarfPropertyAttribute.GetAttribute(pi.ContainedProperty).DisableDeleteCascade)
                                    alterTable += " ON DELETE CASCADE ";

                                addConstraints.AppendLine(alterTable);
                                addConstraints.AppendLine("GO ");

                            }
                        }
                    }
                    else
                    {
                        if (IsColumnNullable(type, pi.ContainedProperty))
                        {
                            addColumns.AppendLine("ALTER TABLE [" + type.Name + "] ADD " + TypeToColumnName(pi) + " " + TypeToColumnConstruction(type, pi.ContainedProperty).TruncateEnd(2));
                            addColumns.AppendLine("GO ");
                        }
                        else
                        {
                            object value = null;

                            try { value = Activator.CreateInstance(pi.PropertyType); }
                            catch { }

                            addColumns.AppendLine("GO ");
                            addColumns.AppendLine("ALTER TABLE [" + type.Name + "] ADD " + TypeToColumnName(pi) + " " + " " + TypeToColumnConstruction(type, pi.ContainedProperty).TruncateEnd(2).Replace("NOT NULL", string.Empty));
                            addColumns.AppendLine("GO ");
                            addColumns.AppendLine("-- WARNING! Value is probably wrong. Correct before you execute! ");
                            addColumns.AppendLine("UPDATE [" + type.Name + "] SET " + TypeToColumnName(pi) + " = " + database.ValueToSqlString(value) + " ");
                            addColumns.AppendLine("GO ");
                            addColumns.AppendLine("ALTER TABLE [" + type.Name + "] ALTER COLUMN " + TypeToColumnName(pi) + " " + TypeToColumnConstruction(type, pi.ContainedProperty, true).TruncateEnd(2));
                            addColumns.AppendLine("GO ");
                        }

                        if (pi.PropertyType.Implements<IDwarf>())
                        {
                            var constraintName = "FK_" + type.Name + "_" + pi.Name;

                            var alterTable = "ALTER TABLE [" + type.Name + "] ADD CONSTRAINT [" + constraintName + "Id] FOREIGN KEY (" + pi.Name + "Id) REFERENCES [" + pi.PropertyType.Name + "] (Id)";

                            if (!DwarfPropertyAttribute.GetAttribute(pi.ContainedProperty).DisableDeleteCascade)
                                alterTable += " ON DELETE CASCADE ";

                            addConstraints.AppendLine(alterTable);
                            addColumns.AppendLine("GO ");
                        }
                    }
                }
            }

            foreach (var existingTable in existingDatabaseTables)
            {
                var uqConstraints = ((List<dynamic>)DwarfContext<T>.GetDatabase().ExecuteCustomQuery<T>("SELECT obj.name FROM sys.objects obj inner join sys.tables t on obj.parent_object_id = t.object_id WHERE obj.type = 'UQ' and t.name = '" + existingTable + "'")).Select(x => x.name);

                foreach (var uqConstraint in uqConstraints)
                {
                    var uqParts = uqConstraint.Split('_');

                    var type = allCurrentDomainTypes.FirstOrDefault(x => x.Name.Equals(uqParts[1]));

                    if (type != null)
                    {
                        var columns = (List<dynamic>)DwarfContext<T>.GetDatabase().ExecuteCustomQuery<T>("select COLUMN_NAME from INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE where CONSTRAINT_NAME = '" + uqConstraint + "'");

                        //Not a unique combination, but a unique column (right?)
                        if (columns.Count == 1)
                        {
                            var pi = ColumnToProperty(type, columns.First().COLUMN_NAME);

                            if (pi != null)
                            {
                                var att = DwarfPropertyAttribute.GetAttribute(pi);

                                if (att != null && !att.IsUnique)
                                    AddDropConstraint(dropPKConstraints, dropFKConstraints, "IF EXISTS (SELECT 1 FROM sys.objects WHERE OBJECT_ID = OBJECT_ID(N'[" + uqConstraint + "]') AND PARENT_OBJECT_ID = OBJECT_ID('[" + type.Name + "]')) ALTER TABLE [" + type.Name + "] DROP CONSTRAINT [" + uqConstraint + "]");
                            }
                        }
                        else
                        {
                            var uqProperties = (IEnumerable<ExpressionProperty>)DwarfHelper.GetUniqueGroupProperties<T>(type, uqParts[2]);

                            if (!uqProperties.Any())
                            {
                                AddDropConstraint(dropPKConstraints, dropFKConstraints, "IF EXISTS (SELECT 1 FROM sys.objects WHERE OBJECT_ID = OBJECT_ID(N'[" + uqConstraint + "]') AND PARENT_OBJECT_ID = OBJECT_ID('[" + type.Name + "]')) ALTER TABLE [" + type.Name + "] DROP CONSTRAINT [" + uqConstraint + "]");
                            }
                            else
                            {
                                var difference = uqProperties.Select(x => x.Name).Except(columns.Select(x => x.COLUMN_NAME));

                                if (difference.Any())
                                {
                                    AddDropConstraint(dropPKConstraints, dropFKConstraints, "IF EXISTS (SELECT 1 FROM sys.objects WHERE OBJECT_ID = OBJECT_ID(N'[" + uqConstraint + "]') AND PARENT_OBJECT_ID = OBJECT_ID('[" + type.Name + "]')) ALTER TABLE [" + type.Name + "] DROP CONSTRAINT [" + uqConstraint + "]");
                                    CreateUniqueConstraint<T>(addConstraints, type);
                                }
                            }
                        }
                    }
                }
            }

            foreach (var type in allCurrentDomainTypes)
            {
                foreach (var pi in DwarfHelper.GetUniqueDBProperties<T>(type))
                {
                    var piName = pi.Name + (pi.PropertyType.Implements<IDwarf>() ? "Id" : string.Empty);
                    var uqName = "UQ_" + type.Name + "_" + piName;

                    var columns = DwarfContext<T>.GetDatabase().ExecuteCustomQuery<T>("select COLUMN_NAME from INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE where CONSTRAINT_NAME = '" + uqName + "'");

                    if (columns.Count == 0 && !addColumns.ToString().Contains(uqName) && !addTables.ToString().Contains(uqName))
                    {
                        addConstraints.AppendLine("ALTER TABLE [" + type.Name + "] ADD CONSTRAINT [" + uqName + "] UNIQUE ([" + pi.Name + "]) ");
                        addConstraints.AppendLine("GO ");
                    }
                }

                foreach (var uniqueGroupName in DwarfHelper.GetUniqueGroupNames<T>(type))
                {
                    var pis = DwarfHelper.GetUniqueGroupProperties<T>(type, uniqueGroupName);

                    var columns = DwarfContext<T>.GetDatabase().ExecuteCustomQuery<T>("select COLUMN_NAME from INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE where CONSTRAINT_NAME = 'UQ_" + type.Name + "_" + uniqueGroupName + "'").Select(x => x.COLUMN_NAME).ToList();

                    if (columns.Any())
                    {
                        var differnce = pis.Select(x => x.Name).Except(columns);

                        if (differnce.Any())
                        {
                            AddDropConstraint(dropPKConstraints, dropFKConstraints, "IF EXISTS (SELECT 1 FROM sys.objects WHERE OBJECT_ID = OBJECT_ID(N'[" + uniqueGroupName + "]') AND PARENT_OBJECT_ID = OBJECT_ID('[" + type.Name + "]')) ALTER TABLE [" + type.Name + "] DROP CONSTRAINT [" + uniqueGroupName + "]");
                            CreateUniqueConstraint<T>(addConstraints, type, uniqueGroupName);
                            addConstraints.AppendLine("GO ");
                        }
                    }
                    else
                    {
                        CreateUniqueConstraint<T>(addConstraints, type, uniqueGroupName);
                        addConstraints.AppendLine("GO ");
                    }
                }

                var uniqueColumns = DwarfContext<T>.GetDatabase().ExecuteCustomQuery<T>("select COLUMN_NAME, CONSTRAINT_NAME from INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE where CONSTRAINT_NAME like 'UQ_%' and CONSTRAINT_NAME like '%_' + COLUMN_NAME and TABLE_NAME = '" + type.Name + "' ");

                foreach (var uniqueColumn in uniqueColumns)
                {
                    var pi = ColumnToProperty(type, uniqueColumn.COLUMN_NAME.ToString());

                    if (pi == null)
                    {
                        AddDropConstraint(dropPKConstraints, dropFKConstraints, "IF EXISTS (SELECT 1 FROM sys.objects WHERE OBJECT_ID = OBJECT_ID(N'[" + uniqueColumn.CONSTRAINT_NAME + "]') AND PARENT_OBJECT_ID = OBJECT_ID('[" + type.Name + "]')) ALTER TABLE [" + type.Name + "] DROP CONSTRAINT [" + uniqueColumn.CONSTRAINT_NAME + "]");
                        continue;
                    }

                    var att = DwarfPropertyAttribute.GetAttribute(pi);

                    if (att == null)
                    {
                        AddDropConstraint(dropPKConstraints, dropFKConstraints, "IF EXISTS (SELECT 1 FROM sys.objects WHERE OBJECT_ID = OBJECT_ID(N'[" + uniqueColumn.CONSTRAINT_NAME + "]') AND PARENT_OBJECT_ID = OBJECT_ID('[" + type.Name + "]')) ALTER TABLE [" + type.Name + "] DROP CONSTRAINT [" + uniqueColumn.CONSTRAINT_NAME + "]");
                        continue;
                    }

                    if (!att.IsUnique)
                        AddDropConstraint(dropPKConstraints, dropFKConstraints, "IF EXISTS (SELECT 1 FROM sys.objects WHERE OBJECT_ID = OBJECT_ID(N'[" + uniqueColumn.CONSTRAINT_NAME + "]') AND PARENT_OBJECT_ID = OBJECT_ID('[" + type.Name + "]')) ALTER TABLE [" + type.Name + "] DROP CONSTRAINT [" + uniqueColumn.CONSTRAINT_NAME + "]");
                }
            }

            
                var result = AppendSection(dropFKConstraints) +
                   AppendSection(dropPKConstraints) +
                   AppendSection(dropColumns) +
                   AppendSection(dropTables) +
                   AppendSection(addTables) +
                   AppendSection(addManyToManyTables) +
                   AppendSection(addColumns) +
                   AppendSection(addConstraints);


            if (!string.IsNullOrEmpty(result.Trim()))
            {

                return "--WARNING--\r\n" +
                       "--Use these scripts with caution as there's no guarantee that all model changes are --\r\n" +
                       "--reflected here nor that any previously persisted data will remain intact post execution. --\r\n" +
                       "\r\n" +
                       result;
            }
            return string.Empty;

        }

        #endregion GetUpdateScript

        #region ColumnToProperty

        private static PropertyInfo ColumnToProperty(Type type, string columnName)
        {
            var pi = type.GetProperty(columnName);

            if (pi == null && columnName.EndsWith("Id"))
                return type.GetProperty(columnName.TruncateEnd(2));

            return pi;
        }

        #endregion ColumnToProperty

        #region DropDeadTableConstraints

        private static void DropDeadTableConstraints<T>(StringBuilder dropPKConstraints, StringBuilder dropFKConstraints, string deadTable)
        {
            var fkContraints = DwarfContext<T>.GetDatabase().ExecuteCustomQuery<T>("SELECT obj.name as name1, t.name as name2, c.name as name3, t2.name as name4 FROM sys.objects obj inner join sys.foreign_key_columns fk on obj.object_id = fk.constraint_object_id inner join sys.columns c on fk.referenced_object_id = c.object_id and fk.referenced_column_id = c.column_id inner join sys.tables t on t.object_id = c.object_id inner join sys.tables t2 on fk.parent_object_id = t2.object_id WHERE obj.type = 'F' and t.name = '" + deadTable + "'");

            foreach (var contraint in fkContraints)
            {
                var fk = contraint.name1.ToString();
                var typeName = contraint.name2.ToString();
                AddDropConstraint(dropPKConstraints, dropFKConstraints, "IF EXISTS (SELECT 1 FROM sys.objects WHERE OBJECT_ID = OBJECT_ID(N'[" + fk + "]') AND PARENT_OBJECT_ID = OBJECT_ID('[" + typeName + "]')) ALTER TABLE [" + typeName + "] DROP CONSTRAINT [" + fk + "]");
            }
        }

        #endregion DropDeadTableConstraints

        #region AppendSection

        private static string AppendSection(StringBuilder sb)
        {
            return sb.Length > 0 ? sb + "\r\n" : string.Empty;
        }

        #endregion AppendSection

        #region GetCurrentManyToManyTables

        private static IEnumerable<string> GetCurrentManyToManyTables(IEnumerable<Type> allTypes)
        {
            foreach (var type in allTypes)
            {
                foreach (var pi in DwarfHelper.GetManyToManyProperties(type))
                {
                    var value = ManyToManyAttribute.GetTableName(type, pi.ContainedProperty);

                    if (!string.IsNullOrEmpty(value))
                        yield return value;
                }
            }
        }

        #endregion GetCurrentManyToManyTables

        #region AddDropConstraint

        /// <summary>
        /// FK drops needs to be executed before PK. This way we can easily separate them)
        /// </summary>
        private static void AddDropConstraint(StringBuilder pkConstraints, StringBuilder fkConstraints, string command)
        {
            if (command.Contains("PK_"))
            {
                pkConstraints.AppendLine(command);
                pkConstraints.AppendLine("GO");
            }
            else
            {
                fkConstraints.AppendLine(command);
                fkConstraints.AppendLine("GO");
            }
        }

        #endregion AddDropConstraint

        #region TypeToColumnType

        /// <summary>
        /// Returns the base sql type for the supplied property
        /// </summary>
        internal static string TypeToColumnType(PropertyInfo pi)
        {
            var value = String.Empty;

            if (pi.Name.Equals("Id"))
                value = "uniqueidentifier";
            else if (DwarfPropertyAttribute.IsFK(pi))
                value = "uniqueidentifier";
            else if (pi.PropertyType.Implements<int?>())
                value = "int";
            else if (pi.PropertyType.Implements<decimal?>())
                value = "decimal";
            else if (pi.PropertyType.Implements<double?>())
                value = "float";
            else if (pi.PropertyType.Implements<DateTime?>())
                value = "datetime";
            else if (pi.PropertyType == typeof(string))
                value = "nvarchar";
            else if (pi.PropertyType.IsEnum())
                value = "nvarchar";
            else if (pi.PropertyType.Implements<bool?>())
                value = "bit";
            else if (pi.PropertyType.Implements<Guid?>())
                value = "uniqueidentifier";
            else if (pi.PropertyType.Implements<IGem>())
                value = "nvarchar";
            else if (pi.PropertyType.Implements<IGemList>())
                value = "nvarchar";
            else if (pi.PropertyType.Implements<Type>())
                value = "nvarchar";
            else if (pi.PropertyType.Implements<byte[]>())
                value = "varbinary";

            if (string.IsNullOrEmpty(value))
                throw new InvalidOperationException(pi.Name + "'s type (" + pi.PropertyType + ") isn't supported.");

            return value;
        }

        #endregion TypeToColumnType

        #region TypeToColumnConstruction

        /// <summary>
        /// Returns the sql needed to construct the supplied property as a column
        /// </summary>
        internal static string TypeToColumnConstruction(Type type, PropertyInfo pi, bool skipConstraint = false)
        {
            var value = TypeToColumnType(pi);

            var att = DwarfPropertyAttribute.GetAttribute(pi);

            if (pi.Name.Equals("Id"))
                value += " NOT NULL";
            else if (DwarfPropertyAttribute.IsFK(pi))
                value += att.IsNullable ? string.Empty : " NOT NULL";
            else if (pi.PropertyType == typeof(string))
                value += att.UseMaxLength ? "(max)" : "(255)";
            else if (pi.PropertyType.Implements<IGem>())
                value += "(255)" + (att.IsNullable ? string.Empty : " NOT NULL");
            else if (pi.PropertyType.Implements<IGemList>())
                value += "(max)";
            else if (pi.PropertyType.Implements<byte[]>())
                value += "(max)";
            else if (pi.PropertyType.Implements<Type>())
                value += "(255)";
            else if (pi.PropertyType.Implements<decimal?>())
                value += "(28, 8)";
            else if (pi.PropertyType.IsEnum())
                value += "(255)" + (pi.PropertyType.IsGenericType ? string.Empty : " NOT NULL");
            else if (!pi.PropertyType.IsGenericType && !att.IsNullable)
                value += " NOT NULL";

            if (att != null && att.IsUnique)
            {
                if (!skipConstraint)
                    value += string.Format(" CONSTRAINT [UQ_{0}_{1}{2}] UNIQUE", type.Name, pi.Name, DwarfPropertyAttribute.RequiresAppendedId(pi) ? "Id" : string.Empty);
                else
                    value += "/* WARNING! You might manually have to drop and recreate any Unique Constraint*/";
            }

            if (string.IsNullOrEmpty(value))
                throw new InvalidOperationException(type.Name + "." + pi.Name + "'s type (" + pi.PropertyType + ") isn't supported.");

            return value + ", ";
        }

        #endregion TypeToColumnConstruction

        #region IsColumnNullable

        /// <summary>
        /// Returns true if the base type of supplied property info is nullable
        /// </summary>
        internal static bool IsColumnNullable(Type type, PropertyInfo pi)
        {
            var att = DwarfPropertyAttribute.GetAttribute(pi);

            if (att == null)
                return true;

            if (att.IsPrimaryKey)
                return false;

            if (pi.Name.Equals("Id"))
                return false;

            if (DwarfPropertyAttribute.IsFK(pi))
                return att.IsNullable;

            if (pi.PropertyType == typeof(string))
                return true;

            if (att.IsNullable)
                return true;

            return pi.PropertyType.IsGenericType;
        }

        #endregion IsColumnNullable

        #region TypeToColumnName

        /// <summary>
        /// Extracts the column name from the supplied property
        /// </summary>
        internal static object TypeToColumnName(PropertyInfo pi)
        {
            return DwarfPropertyAttribute.RequiresAppendedId(pi) ? "[" + pi.Name + "Id]" : "[" + pi.Name + "]";
        }

        /// <summary>
        /// Extracts the column name from the supplied property
        /// </summary>
        internal static object TypeToColumnName(ExpressionProperty pi)
        {
            return TypeToColumnName(pi.ContainedProperty);
        }

        #endregion TypeToColumnName

        #region CreateTable

        /// <summary>
        /// Generates create table commands for the supplied type
        /// </summary>
        internal static void CreateTable<T>(StringBuilder tables, Type type, StringBuilder constraints, StringBuilder manyToManyTables)
        {
            tables.AppendLine("CREATE TABLE [" + type.Name + "] ( ");

            foreach (var pi in DwarfHelper.GetDBProperties(type))
                tables.AppendLine(TypeToColumnName(pi) + " " + TypeToColumnConstruction(type, pi.ContainedProperty));

            foreach (var pi in DwarfHelper.GetGemListProperties(type))
                tables.AppendLine(TypeToColumnName(pi) + " " + TypeToColumnConstruction(type, pi.ContainedProperty));

            foreach (var manyToManyProperty in DwarfHelper.GetManyToManyProperties(type))
                CreateManyToManyTable(type, manyToManyProperty, manyToManyTables, constraints);

            var keys = String.Empty;

            foreach (var propertyInfo in DwarfHelper.GetPKProperties(type))
                keys += TypeToColumnName(propertyInfo) + ", ";

            if (!string.IsNullOrEmpty(keys))
            {
                keys = keys.TruncateEnd(2);

                keys = "CONSTRAINT [PK_" + type.Name + "] PRIMARY KEY (" + keys + ")";

                tables.AppendLine(keys);
            }

            foreach (var pi in DwarfHelper.GetFKProperties<T>(type))
            {
                var constraintName = "FK_" + type.Name + "_" + pi.Name;

                var alterTable = "ALTER TABLE [" + type.Name + "] ADD CONSTRAINT [" + constraintName + "Id] FOREIGN KEY (" +
                                 pi.Name + "Id) REFERENCES [" + pi.PropertyType.Name + "] (Id)";

                if (!DwarfPropertyAttribute.GetAttribute(pi.ContainedProperty).DisableDeleteCascade)
                    alterTable += " ON DELETE CASCADE ";

                constraints.AppendLine(alterTable);
            }
            CreateUniqueConstraint<T>(constraints, type);

            tables.AppendLine(") ");
            tables.AppendLine();
        }

        #endregion CreateTable

        #region CreateUniqueConstraint

        /// <summary>
        /// Creates unique constraints for the supplied type
        /// </summary>
        internal static void CreateUniqueConstraint<T>(StringBuilder constraints, Type type)
        {
            foreach (var uniqueGroupName in DwarfHelper.GetUniqueGroupNames<T>(type))
                CreateUniqueConstraint<T>(constraints, type, uniqueGroupName);
        }

        /// <summary>
        /// Creates unique constraints for the supplied type and group name
        /// </summary>
        internal static void CreateUniqueConstraint<T>(StringBuilder constraints, Type type, string uniqueGroupName)
        {
            var constraintName = "UQ_" + type.Name + "_" + uniqueGroupName;

            var columns = DwarfHelper.GetUniqueGroupProperties<T>(type, uniqueGroupName).Select(x => QueryBuilder.GetColumnName(x.ContainedProperty)).
                Aggregate(String.Empty, (s, x) => s + ", [" + x + "]").Remove(0, 2);

            var alterTable = "ALTER TABLE [" + type.Name + "] ADD CONSTRAINT [" + constraintName + "] UNIQUE (" + columns + ")";

            constraints.AppendLine(alterTable);
        }

        #endregion CreateUniqueConstraint

        #region CreateManyToManyTable

        /// <summary>
        /// Generates create table commands for the supplied type
        /// </summary>
        internal static void CreateManyToManyTable(Type type, ExpressionProperty pi, StringBuilder manyToManyTables, StringBuilder foreignKeys)
        {
            var nameOwner = type.Name;
            var nameChild = pi.PropertyType.GetGenericArguments()[0].Name;

            var tableName = ManyToManyAttribute.GetTableName(type, pi.ContainedProperty);

            if (manyToManyTables.ToString().Contains("CREATE TABLE [" + tableName + "] ( "))
                return;

            manyToManyTables.AppendLine("CREATE TABLE [" + tableName + "] ( ");
            manyToManyTables.AppendLine("[" + nameOwner + "Id] [uniqueidentifier] NOT NULL, ");
            manyToManyTables.AppendLine("[" + nameChild + "Id] [uniqueidentifier] NOT NULL, ");
            manyToManyTables.AppendLine("CONSTRAINT [PK_" + tableName + "] PRIMARY KEY (" + nameOwner + "Id, " + nameChild + "Id)");
            manyToManyTables.AppendLine(")");
            manyToManyTables.AppendLine();

            var ownerConstraintName = "FK_" + tableName + "_" + nameOwner;
            var childConstraintName = "FK_" + tableName + "_" + nameChild;

            foreignKeys.AppendLine("ALTER TABLE [" + tableName + "] ADD CONSTRAINT [" + ownerConstraintName + "Id] FOREIGN KEY (" + nameOwner + "Id) REFERENCES [" + nameOwner + "] (Id) ON DELETE CASCADE ");
            foreignKeys.AppendLine("ALTER TABLE [" + tableName + "] ADD CONSTRAINT [" + childConstraintName + "Id] FOREIGN KEY (" + nameChild + "Id) REFERENCES [" + nameChild + "] (Id) ON DELETE CASCADE ");
        }

        #endregion CreateManyToManyTable
    }
}
