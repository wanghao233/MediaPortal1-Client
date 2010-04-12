/*
 * Encapsulation of a single field (property/column relationship)
 * Copyright (C) 2004 Morten Mertner
 * 
 * This library is free software; you can redistribute it and/or modify it 
 * under the terms of the GNU Lesser General Public License 2.1 or later, as
 * published by the Free Software Foundation. See the included License.txt
 * or http://www.gnu.org/copyleft/lesser.html for details.
 *
 * $Id: FieldMap.cs 1241 2008-04-21 14:49:35Z mm $
 */

using System;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Reflection;
using System.Text;
using Gentle.Common;
using TypeConverter=Gentle.Common.TypeConverter;

namespace Gentle.Framework
{
	/// <summary>
	/// A small structure for storing metadata for table columns (fields).
	/// </summary>
	/// <remarks>
	/// </remarks>
	public class FieldMap
	{
		private const long NO_DBTYPE = -1;
		private bool handleEnumAsString;
		private TableMap map; // the map to which this instance belongs
		private MemberInfo memberInfo;
		private Type memberType;
		// database information
		private string columnName;
		private bool isReservedWord = false; // unused?!
		private bool isNullable;
		private bool isAutoGenerated; // true for values assigned by database on insert
		private string sequenceName;
		private bool isPrimaryKey;
		private string foreignKeyTableName;
		private string foreignKeyColumnName;
		// behavioral settings
		private object nullValue;
		private bool isConcurrencyColumn;
		private bool isSoftDeleteColumn;
		// the actual database type of the field (provider-specific enum converted to long)
		private long dbType = NO_DBTYPE;
		private int size; // the database field size/length
		private int columnId; // the database-internal column id 
		private bool isReadOnly;
		private bool isUpdateAfterWrite;

		#region Constructor(s)
		/// <summary>
		/// Constructor for fields using information obtained from the TableColumn attribute.
		/// </summary>
		public FieldMap( ObjectMap map, MemberAttributeInfo memberAttributeInfo )
		{
			this.map = map;
			memberInfo = memberAttributeInfo.MemberInfo;
			if( memberInfo is PropertyInfo )
			{
				memberType = (memberInfo as PropertyInfo).PropertyType;
			}
			else
			{
				memberType = (memberInfo as FieldInfo).FieldType;
			}
			// extract information from attributes
			if( memberAttributeInfo.Attributes != null )
			{
				foreach( Attribute attr in memberAttributeInfo.Attributes )
				{
					if( attr is TableColumnAttribute )
					{
						TableColumnAttribute tc = attr as TableColumnAttribute;
						SetColumnName( tc.Name ?? memberInfo.Name );
						SetNullValue( tc.NullValue );
						SetIsNullable( ! tc.NotNull );
						SetSize( tc.Size );
						if( tc.HasDbType )
						{
							SetDbType( tc.DatabaseType );
						}
						else
						{
							SetDbType( NO_DBTYPE );
						}
						handleEnumAsString = tc.HandleEnumAsString;
						isReadOnly = tc.IsReadOnly;
						isUpdateAfterWrite = tc.IsUpdateAfterWrite;
					}
					else if( attr is PrimaryKeyAttribute )
					{
						PrimaryKeyAttribute pk = attr as PrimaryKeyAttribute;
						SetIsPrimaryKey( true );
						SetIsAutoGenerated( pk.AutoGenerated );
						if( IsAutoGenerated )
						{
							map.IdentityMap = this;
						}
					}
					else if( attr is ForeignKeyAttribute )
					{
						ForeignKeyAttribute fk = attr as ForeignKeyAttribute;
						SetForeignKeyTableName( fk.ForeignTable );
						SetForeignKeyColumnName( fk.ForeignColumn );
					}
					else if( attr is ConcurrencyAttribute )
					{
						ConcurrencyAttribute ca = attr as ConcurrencyAttribute;
						Check.Verify( memberType == typeof(int) || memberType == typeof(long),
						              Error.DeveloperError, "Unsupported property type {0} for ConcurrencyAttribute {1}",
						              memberInfo.ReflectedType, memberInfo.Name );
						isConcurrencyColumn = true;
						map.ConcurrencyMap = this;
					}
					else if( attr is SoftDeleteAttribute )
					{
						SoftDeleteAttribute sd = attr as SoftDeleteAttribute;
						Check.Verify( memberType == typeof(int) || memberType == typeof(long),
						              Error.DeveloperError, "Unsupported property type {0} for SoftDeleteAttribute {1}",
						              memberInfo.ReflectedType, memberInfo.Name );
						isSoftDeleteColumn = true;
						map.SoftDeleteMap = this;
					}
					else if( attr is SequenceNameAttribute )
					{
						// sequence name used when available (in place of name conventions or autodetected value)
						sequenceName = (attr as SequenceNameAttribute).Name;
					}
					else if( attr is InheritanceAttribute )
					{
						// sequence name used when available (in place of name conventions or autodetected value)
						map.InheritanceMap = this;
					}
				}
			}
		}

		/// <summary>
		/// Constructor for fields using information obtained directly from the database (no property info).
		/// </summary>
		public FieldMap( TableMap map, string columnName )
		{
			this.map = map;
			SetColumnName( columnName );
		}
		#endregion

		#region Internal Methods
		public void SetColumnName( string columnName )
		{
			this.columnName = columnName.Trim();
		}

		public void SetDbType( long dbType )
		{
			this.dbType = dbType;
		}

		public void SetDbType( DbType dbType )
		{
			this.dbType = (long) dbType;
		}

		public void SetDbType( string dbType, bool isUnsigned )
		{
			GentleSqlFactory sf = map.Provider != null ? map.Provider.GetSqlFactory() : Broker.GetSqlFactory();
			try
			{
				long dbt = sf.GetDbType( dbType, isUnsigned );
				if( dbt != NO_DBTYPE )
				{
					this.dbType = dbt;
				}
			}
			catch( GentleException fe )
			{
				// determine field types from the property type if it hasn't been set and
				// the actual database column is unknown.. 
				// TODO maybe we should just throw an error rather than hope this will work ;)
				if( fe.Error == Error.UnsupportedColumnType && this.dbType == NO_DBTYPE && memberType != null )
				{
					this.dbType = sf.GetDbType( memberType );
				}
			}
		}

		public void SetIsNullable( bool isNullable )
		{
			this.isNullable = isNullable;
			// update fieldmap with proper NullValue (for null handling of datetime fields)
			// this is required because DateTime/decimal is not a const and thus cannot normally be 
			// assigned to the NullValue property
			if( isNullable && memberType != null && nullValue == null )
			{
				if( memberType.Equals( typeof(DateTime) ) )
				{
					nullValue = DateTime.MinValue;
				}
				else if( memberType.Equals( typeof(Decimal) ) )
				{
					nullValue = Decimal.MinValue;
				}
			}
		}

		public void SetSize( int size )
		{
			this.size = size;
		}

		internal void SetNullValue( object nullValue )
		{
			if( nullValue != null )
			{
				// nullValue must be specified using NullOption for these types only
				if( memberType == typeof(DateTime) || memberType == typeof(Decimal) || memberType == typeof(Guid) )
				{
					NullOption nv = (NullOption) nullValue;
					switch( memberType.Name )
					{
						case "DateTime":
						{
							switch( nv )
							{
								case NullOption.MinValue:
									this.nullValue = DateTime.MinValue;
									break;
								case NullOption.MaxValue:
									this.nullValue = DateTime.MaxValue;
									break;
								default:
									throw new GentleException( Error.DeveloperError,
									                           String.Format( "Invalid NullValue on field {0} of type {1}.", MemberName, memberType ) );
							}
							break;
						}
						case "Decimal":
						{
							switch( nv )
							{
								case NullOption.MinValue:
									this.nullValue = Decimal.MinValue;
									break;
								case NullOption.MaxValue:
									this.nullValue = Decimal.MaxValue;
									break;
								case NullOption.Zero:
									this.nullValue = Decimal.Zero;
									break;
								default:
									throw new GentleException( Error.DeveloperError,
									                           String.Format( "Invalid NullValue on field {0} of type {1}.", MemberName, memberType ) );
							}
							break;
						}
						case "Guid":
						{
							switch( nv )
							{
								case NullOption.EmptyGuid:
									this.nullValue = Guid.Empty;
									break;
								default:
									throw new GentleException( Error.DeveloperError,
									                           String.Format( "Invalid NullValue on field {0} of type {1}.", MemberName, memberType ) );
							}
							break;
						}
						default:
							this.nullValue = Convert.ChangeType( nullValue, memberType );
							break;
					}
				}
				else
				{
					this.nullValue = Convert.ChangeType( nullValue, memberType );
				}
			}
			else
			{
				this.nullValue = null;
			}
		}

		public void SetIsAutoGenerated( bool isAutoGenerated )
		{
			this.isAutoGenerated = isAutoGenerated;
			if( isAutoGenerated && IsPrimaryKey )
			{
				map.IdentityMap = this;
			}
		}

		public void SetIsPrimaryKey( bool isPrimaryKey )
		{
			this.isPrimaryKey = isPrimaryKey;
		}

		public void SetForeignKeyTableName( string foreignKeyTableName )
		{
			GentleSqlFactory sf = map.Provider != null ? map.Provider.GetSqlFactory() : Broker.GetSqlFactory();
			foreignKeyTableName = foreignKeyTableName.Trim();
			if( sf.IsReservedWord( foreignKeyTableName ) )
			{
				this.foreignKeyTableName = sf.QuoteReservedWord( foreignKeyTableName );
			}
			else
			{
				this.foreignKeyTableName = foreignKeyTableName;
			}
		}

		public void SetForeignKeyColumnName( string foreignKeyColumnName )
		{
			GentleSqlFactory sf = map.Provider != null ? map.Provider.GetSqlFactory() : Broker.GetSqlFactory();
			foreignKeyColumnName = foreignKeyColumnName.Trim();
			if( sf.IsReservedWord( foreignKeyColumnName ) )
			{
				this.foreignKeyColumnName = sf.QuoteReservedWord( foreignKeyColumnName );
			}
			else
			{
				this.foreignKeyColumnName = foreignKeyColumnName;
			}
		}
		#endregion

		#region Internal Properties
		public int ColumnId
		{
			get { return columnId; }
			set { columnId = value; }
		}
		public bool HasDbType
		{
			get { return dbType != NO_DBTYPE; }
		}
		internal bool IsConcurrencyColumn
		{
			get { return isConcurrencyColumn; }
			set { isConcurrencyColumn = value; }
		}
		internal bool IsSoftDeleteColumn
		{
			get { return isSoftDeleteColumn; }
			set { isSoftDeleteColumn = value; }
		}
		#endregion

		#region Properties
		/// <summary>
		/// The (unquoted) column name this instance represents.
		/// </summary>
		public string ColumnName
		{
			get { return columnName; }
		}
		/// <summary>
		/// The (reserved word quoted) column name this instance represents.
		/// </summary>
		public string QuotedColumnName
		{
			get
			{
				GentleSqlFactory sf = map.Provider != null ? map.Provider.GetSqlFactory() : Broker.GetSqlFactory();
				if( sf.IsReservedWord( columnName ) )
				{
					// return a quoted version of the column name 
					return sf.QuoteReservedWord( columnName );
				}
				return columnName;
			}
		}
		/// <summary>
		/// The column name this instance represents prefixed by the table to which it belongs ("table.column").
		/// </summary>
		public string TableColumnName
		{
			get { return map.TableName + "." + columnName; }
		}
		/// <summary>
		/// The name of the field or property this instance represents.
		/// </summary>
		public string MemberName
		{
			get { return memberInfo.Name; }
		}
		/// <summary>
		/// The type of member (as a value of the MemberTypes enum) this instance represents.
		/// </summary>
		public MemberTypes MemberType
		{
			get { return memberInfo.MemberType; }
		}
		/// <summary>
		/// The member (field or property) type to which this instance belongs.
		/// </summary>
		public Type Type
		{
			get { return memberType; }
		}
		/// <summary>
		/// The database type of this field (the rdbms-specific type enumeration converted to long).
		/// </summary>
		public long DbType
		{
			get { return dbType; }
		}
		/// <summary>
		/// A boolean indicating whether the name of this field is reserved and needs special quoting.
		/// </summary>
		public bool IsReservedWord
		{
			get { return isReservedWord; }
		}
		/// <summary>
		/// The size of this field or 0 if not applicable or unknown.
		/// </summary>
		public int Size
		{
			get { return size; }
		}
		/// <summary>
		/// A boolean indicating whether the database column accepts nulls.
		/// </summary>
		public bool IsNullable
		{
			get { return isNullable; }
		}
		/// <summary>
		/// A boolean indicating whether a null value can be assigned to the property.
		/// </summary>
		public bool IsNullAssignable
		{
			get { return isNullable && (IsGenericNullableType || ! IsValueType); }
		}
		/// <summary>
		/// A boolean indicating whether the property type of this field is a value type.
		/// </summary>
		public bool IsValueType
		{
			get { return memberType.IsValueType; }
		}
		/// <summary>
		/// A boolean indicating whether the type of this field is derived from NullableType.
		/// </summary>
		public bool IsGenericNullableType
		{
			get { return memberType.IsGenericType && memberType.GetGenericTypeDefinition() == typeof(Nullable<>); }
		}
		/// <summary>
		/// A boolean indicating whether this field is autogenerated by the database on insert.
		/// </summary>
		public bool IsAutoGenerated
		{
			get { return isAutoGenerated; }
		}
		/// <summary>
		/// A boolean indicating whether this is a primary key field.
		/// </summary>
		public bool IsPrimaryKey
		{
			get { return isPrimaryKey; }
		}
		/// <summary>
		/// A boolean indicating whether this autogenerated key field is of a type supported 
		/// by Gentle.
		/// </summary>
		public bool IsAutoGeneratedKeyAndSupportedType
		{
			get
			{
				bool result = memberType == typeof(int) || memberType == typeof(long);
				return result && IsPrimaryKey && IsAutoGenerated;
			}
		}
		/// <summary>
		/// A boolean indicating whether this is a foreign key field.
		/// </summary>
		public bool IsForeignKey
		{
			get { return foreignKeyColumnName != null && foreignKeyTableName != null; }
		}
		/// <summary>
		/// The name of the table for foreign keys and null otherwise.
		/// </summary>
		public string ForeignKeyTableName
		{
			get { return foreignKeyTableName; }
		}
		/// <summary>
		/// The name of the table column for foreign keys and null otherwise.
		/// </summary>
		public string ForeignKeyColumnName
		{
			get { return foreignKeyColumnName; }
		}
		/// <summary>
		/// The NULL value used when performing automatic translation between 
		/// a value type and database nulls.
		/// </summary>
		public object NullValue
		{
			get
			{
				if( memberType == typeof(Guid) )
				{
					return Guid.Empty;
				}
				return nullValue;
			}
		}
		[Obsolete( "This property has been renamed into NullValue." )]
		public object MagicValue
		{
			get { return NullValue; }
		}
		/// <summary>
		/// The MemberInfo instance of the member represented by this instance.
		/// </summary>
		public MemberInfo MemberInfo
		{
			get { return memberInfo; }
		}
		/// <summary>
		/// The name of the database sequence for autogenerated columns, usually the primary key column.
		/// </summary>
		public string SequenceName
		{
			get { return sequenceName; }
		}

		/// <summary>
		/// If this field info represents enum then this property indicates 
		/// wheter it is saved as string or as integer.
		/// Default is false, ie enums are saved as integers
		/// </summary>
		public bool HandleEnumAsString
		{
			get { return handleEnumAsString; }
			set { handleEnumAsString = value; }
		}

		/// <summary>
		/// This value indicates that the column should not be set on insert and update. It is
		/// primarily useful for columns that are set internally by the database.
		/// </summary>
		public bool IsReadOnly
		{
			get { return isReadOnly; }
			set { isReadOnly = value; }
		}
		/// <summary>
		/// This value indicates that the column must be read after each insert and update. It is
		/// primarily useful for columns that are set internally by the database.
		/// </summary>
		public bool IsUpdateAfterWrite
		{
			get { return isUpdateAfterWrite; }
		}
		#endregion

		public bool IsNull( object val )
		{
			if( val == null || val == DBNull.Value )
			{
				return true;
			}
			// WARNING !!!
			// == comparison result does not match that of .Equals
			//bool BROKEN1 = NullValue != null && val == NullValue && val.GetType() == NullValue.GetType();		
			//bool BROKEN2 = NullValue != null && NullValue == val && val.GetType() == NullValue.GetType();		
			bool WORKS1 = NullValue != null && NullValue.Equals( val ) && val.GetType() == NullValue.GetType();
			//bool WORKS2 = NullValue != null && val.Equals( NullValue ) && val.GetType() == NullValue.GetType();		
			bool isSameValue = IsValueType && val.Equals( NullValue );
			return WORKS1 || isSameValue;
		}

		/// <summary>
		/// Get the value of the property for this FieldMap from the given instance. 
		/// </summary>
		/// <param name="instance">The object whose property value will be retrieved.</param>
		/// <returns>The value of the property.</returns>
		public object GetValue( object instance )
		{
			if( memberInfo is PropertyInfo )
			{
				return (memberInfo as PropertyInfo).GetValue( instance, null );
			}
			return (memberInfo as FieldInfo).GetValue( instance );
		}

		/// <summary>
		/// Set the value of the property for this FieldMap on the given instance. 
		/// </summary>
		/// <param name="instance">The object whose property value will be set.</param>
		/// <param name="value">The value to assign to the property.</param>
		public void SetValue( object instance, object value )
		{
			if( IsNull( value ) )
			{
				value = NullValue;
				Check.Verify( IsNullAssignable || NullValue != null, Error.NullWithNoNullValue, columnName,
				              map is ObjectMap ? (map as ObjectMap).Type.Name : map.TableName );
			}
				// perform a few automatic type conversions
			else if( memberType.IsEnum )
			{
				value = handleEnumAsString
				        	? Enum.Parse( memberType, Convert.ToString( value ), true )
				        	: Enum.ToObject( memberType, Convert.ToInt32( value ) );
			}
			else if( memberType == typeof(Guid) && value != null && value.GetType() == typeof(string) )
			{
				if( Size == 16 ) // binary compressed version
				{
					value = TypeConverter.ToGuid( (string) value );
				}
				else
				{
					value = new Guid( (string) value );
				}
			}
			else if( memberType == typeof(decimal) && value != null && value.GetType() == typeof(string) )
			{
				value = Convert.ToDecimal( value, NumberFormatInfo.InvariantInfo );
			}
			else
			{
				System.ComponentModel.TypeConverter typeConv = TypeDescriptor.GetConverter( memberType );
				if( typeConv != null && typeConv.CanConvertFrom( value.GetType() ) )
				{
					value = typeConv.ConvertFrom( value );
				}
				else
				{
					// check for the existence of a TypeConverterAttribute for the field/property
					object[] attrs = MemberInfo.GetCustomAttributes( typeof(TypeConverterAttribute), false );
					if( attrs.Length == 1 )
					{
						TypeConverterAttribute tca = (TypeConverterAttribute) attrs[ 0 ];
						System.ComponentModel.TypeConverter typeConverter = (System.ComponentModel.TypeConverter)
						                                                    Activator.CreateInstance( Type.GetType( tca.ConverterTypeName ) );
						if( value != null && typeConverter != null && typeConverter.CanConvertFrom( value.GetType() ) )
						{
							value = typeConverter.ConvertFrom( value );
						}
						else
						{
							value = Convert.ChangeType( value, memberType );
						}
					}
					else
					{
						value = Convert.ChangeType( value, memberType );
					}
				}
			}
			// update member
			if( memberInfo is PropertyInfo )
			{
				(memberInfo as PropertyInfo).SetValue( instance, value, Reflector.InstanceCriteria, null, null, null );
			}
			else
			{
				(memberInfo as FieldInfo).SetValue( instance, value, Reflector.InstanceCriteria, null, null );
			}
		}

		/// <summary>
		/// Override the <see cref="ToString"/> method to produce a human readable
		/// representation of the <see cref="FieldMap"/> object.
		/// </summary>
		/// <returns>The string representation of the object.</returns>
		public override string ToString()
		{
			StringBuilder msg = new StringBuilder();
			msg.Append( "Field Map: " ).Append( Environment.NewLine );
			msg.Append( "; ObjectMap: " ).Append( map );
			msg.Append( "; property name: " ).Append( MemberName );
			msg.Append( "; column name: " ).Append( columnName );
			msg.Append( "; type: " ).Append( memberType );
			msg.Append( "; isReservedWord: " ).Append( isReservedWord );
			msg.Append( "; isNullable: " ).Append( isNullable );
			msg.Append( "; isNullAssignable: " ).Append( IsNullAssignable );
			msg.Append( "; isGenericNullableType: " ).Append( IsGenericNullableType );
			msg.Append( "; isAutoGenerated: " ).Append( isAutoGenerated );
			msg.Append( "; isPrimaryKey: " ).Append( isPrimaryKey );
			msg.Append( "; foreignKeyTableName: " ).Append( foreignKeyTableName );
			msg.Append( "; foreignKeyColumnName: " ).Append( foreignKeyColumnName );
			msg.Append( "; nullValue: " ).Append( nullValue );
			//msg.Append("; isConcurrencyControlColumn: ").Append(IsConcurrencyColumn);
			//msg.Append("; propertyInfo: ").Append(propertyInfo);
			msg.Append( "; dbType: " ).Append( dbType );
			msg.Append( "; size: " ).Append( size );
			msg.Append( "; columnId: " ).Append( columnId );
			return msg.ToString();
		}
	}
}