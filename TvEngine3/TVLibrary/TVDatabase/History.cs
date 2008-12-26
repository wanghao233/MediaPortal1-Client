//========================================================================
// This file was generated using the MyGeneration tool in combination
// with the Gentle.NET Business Entity template, $Rev: 965 $
//========================================================================
using System;
using System.Collections;
using Gentle.Framework;
using TvLibrary.Log;

namespace TvDatabase
{
  /// <summary>
  /// Instances of this class represent the properties and methods of a row in the table <b>History</b>.
  /// </summary>
  [TableName("History")]
  public class History : Persistent
  {
    #region Members
    private bool isChanged;
    [TableColumn("idHistory", NotNull = true), PrimaryKey(AutoGenerated = true)]
    private int idHistory;
    [TableColumn("idChannel", NotNull = true)]
    private int idChannel;
    [TableColumn("startTime", NotNull = true)]
    private DateTime startTime;
    [TableColumn("endTime", NotNull = true)]
    private DateTime endTime;
    [TableColumn("title", NotNull = true)]
    private string title;
    [TableColumn("description", NotNull = true)]
    private string description;
    [TableColumn("genre", NotNull = true)]
    private string genre;
    [TableColumn("recorded", NotNull = true)]
    private bool recorded;
    [TableColumn("watched", NotNull = true)]
    private int watched;

    private readonly DateTime _timeStart;
    #endregion

    #region Constructors
    /// <summary> 
    /// Create a new object by specifying all fields (except the auto-generated primary key field). 
    /// </summary> 
    public History(int idChannel, DateTime startTime, DateTime endTime, string title, string description, string genre, bool recorded, int watched)
    {
      isChanged = true;
      this.idChannel = idChannel;
      this.startTime = startTime;
      this.endTime = endTime;
      this.title = title;
      this.description = description;
      this.genre = genre;
      this.recorded = recorded;
      this.watched = watched;
      _timeStart = DateTime.Now;
    }

    /// <summary> 
    /// Create an object from an existing row of data. This will be used by Gentle to 
    /// construct objects from retrieved rows. 
    /// </summary> 
    public History(int idHistory, int idChannel, DateTime startTime, DateTime endTime, string title, string description, string genre, bool recorded, int watched)
    {
      this.idChannel = idChannel;
      this.idHistory = idHistory;
      this.startTime = startTime;
      this.endTime = endTime;
      this.title = title;
      this.description = description;
      this.genre = genre;
      this.recorded = recorded;
      this.watched = watched;
      _timeStart = DateTime.Now;
    }
    #endregion

    #region Public Properties
    /// <summary>
    /// Indicates whether the entity is changed and requires saving or not.
    /// </summary>
    public bool IsChanged
    {
      get { return isChanged; }
    }
    /// <summary>
    /// Property relating to database column idHistory
    /// </summary>
    public int IdHistory
    {
      get { return idHistory; }
    }

    /// <summary>
    /// Property relating to database column idHistory
    /// </summary>
    public int IdChannel
    {
      get { return idChannel; }
      set { isChanged |= idChannel != value; idChannel = value; }
    }

    /// <summary>
    /// Property relating to database column name
    /// </summary>
    public string Title
    {
      get { return title; }
      set { isChanged |= title != value; title = value; }
    }
    /// <summary>
    /// Property relating to database column name
    /// </summary>
    public string Description
    {
      get { return description; }
      set { isChanged |= description != value; description = value; }
    }
    /// <summary>
    /// Property relating to database column name
    /// </summary>
    public string Genre
    {
      get { return genre; }
      set { isChanged |= genre != value; genre = value; }
    }

    /// <summary>
    /// Property relating to database column Recorded
    /// </summary>
    public bool Recorded
    {
      get { return recorded; }
      set { isChanged |= recorded != value; recorded = value; }
    }

    /// <summary>
    /// Property relating to database column isTv
    /// </summary>
    public int Watched
    {
      get { return watched; }
      set { isChanged |= watched != value; watched = value; }
    }

    /// <summary>
    /// Property relating to database column lastGrabTime
    /// </summary>
    public DateTime StartTime
    {
      get { return startTime; }
      set { isChanged |= startTime != value; startTime = value; }
    }

    /// <summary>
    /// Property relating to database column lastGrabTime
    /// </summary>
    public DateTime EndTime
    {
      get { return endTime; }
      set { isChanged |= endTime != value; endTime = value; }
    }
    #endregion


    #region Storage and Retrieval

    /// <summary>
    /// Static method to retrieve all instances that are stored in the database in one call
    /// </summary>
    public static IList ListAll()
    {
      return Broker.RetrieveList(typeof(History));
    }

    /// <summary>
    /// Retrieves an entity given it's id.
    /// </summary>
    public static History Retrieve(int id)
    {
      // Return null if id is smaller than seed and/or increment for autokey
      if (id < 1)
      {
        return null;
      }
      Key key = new Key(typeof(History), true, "idHistory", id);
      return Broker.RetrieveInstance(typeof(History), key) as History;
    }

    /// <summary>
    /// Retrieves an entity given it's id, using Gentle.Framework.Key class.
    /// This allows retrieval based on multi-column keys.
    /// </summary>
    public static History Retrieve(Key key)
    {
      return Broker.RetrieveInstance(typeof(History), key) as History;
    }

    /// <summary>
    /// Persists the entity if it was never persisted or was changed.
    /// </summary>
    public override void Persist()
    {
      if (IsChanged || !IsPersisted)
      {
        try
        {
          base.Persist();
        }
        catch (Exception ex)
        {
          Log.Error("Exception in History.Persist() with Message {0}", ex.Message);
          return;
        }
        isChanged = false;
      }
    }

    #endregion

    #region Relations
    /// <summary>
    /// Get a list of Channel referring to the current entity.
    /// </summary>
    public IList ReferringChannel()
    {
      //select * from 'foreigntable'
      SqlBuilder sb = new SqlBuilder(StatementType.Select, typeof(Channel));

      // where foreigntable.foreignkey = ourprimarykey
      sb.AddConstraint(Operator.Equals, "idChannel", idChannel);

      // passing true indicates that we'd like a list of elements, i.e. that no primary key
      // constraints from the type being retrieved should be added to the statement
      SqlStatement stmt = sb.GetStatement(true);

      // execute the statement/query and create a collection of User instances from the result set
      return ObjectFactory.GetCollection(typeof(Channel), stmt.Execute());

      // TODO In the end, a GentleList should be returned instead of an arraylist
      //return new GentleList( typeof(Channel), this );
    }
    #endregion

    /// <summary>
    /// Saves this instance.
    /// </summary>
    public void Save()
    {
      TimeSpan ts = DateTime.Now - _timeStart;
      watched = (int)ts.TotalMinutes;
      if (watched >= 10)
      {
        Persist();
      }
    }
  }
}
