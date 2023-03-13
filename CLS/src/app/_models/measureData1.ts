//Interface for Measure Data Model
export interface MeasureData {
    measureDataId?: string;
    measureId?: string;
    calendarId?: string;
    targetId?: string;
    value?: string;
    explanation?: string;
    action?: string;
    userId?: string;
    lastUpdatedOn?: string;
    isProcessed?: string
}

/*export interface MeasureDataData
{
  public long Id { set; get; }
  public Measure Measure { set; get; }
  public long MeasureId { set; get; }
  public Calendar Calendar { set; get; }
  public int CalendarId { set; get; }
  public Target Target { get; set; }    
  public long TargetId { set; get; }
  public double? Value { set; get; }
  public string Explanation { set; get; }
  public string Action { set; get; }
  public int? UserId { set; get; }
  public Byte IsProcessed { get; set; }
  public DateTime LastUpdatedOn { set; get; }
}*/

export interface MeasureDataIndexListObject{
    range: string;
    calendarId?: string;
    allow?: boolean;
    editValue?: boolean;
    locked?: boolean;
    confirmed?: boolean;
    filter?: any;
    data?: MeasureData[];
    error?: string;
} 


/*public class MeasureDataFilterReceiveObject
{
  public int? intervalId { set; get; }
  public int? year { set; get; }
  public bool? isDataImport { get; set; }
}

public class MeasureDataReceiveObject
{
  public int? calendarId { set; get; }
  public string day { set; get; } 
  public int hierarchyId { set; get; }
  public int measureTypeId { set; get; }
  public long? measureDataId { set; get; }      
  public double? measureValue { set; get; }
  public string explanation { set; get; }
  public string action { set; get; }   
  //public ErrorModel error { set; get; }
}

public class MeasureDataIndexListObject
  {
    public string range { set; get; }
    public int? calendarId { set; get; }
    public bool allow { set; get; }
    public bool editValue { set; get; }
    public bool locked { set; get;}  
    public bool confirmed { get; set; }   
    public FilterSaveObject filter { set; get; }      
    public List<MeasureDataReturnObject> data { set; get; }
    public ErrorModel error { set; get; }
    }

    public class MeasureDataReturnObject
    {
      public long id { set; get; }
      public string name { set; get; }
      public double? value { set; get; }
      public string explanation { set; get; }
      public string action { set; get; }
      public double? target { set; get; }
      public int? targetCount { get; set; }
      public long? targetId { get; set; }
      public int unitId { set; get; }
      public string units { set; get; }
      public double? yellow { set; get; }
      public string expression { set; get; }
      public string evaluated { set; get; }
      public bool calculated { set; get; }
      public string description { set; get; }
      public UpdatedObject updated { set; get; }
  
    }

public string range { set; get; }
public int? calendarId { set; get; }
public bool allow { set; get; }
public bool editValue { set; get; }
public bool locked { set; get;}  
public bool confirmed { get; set; }   
public FilterSaveObject filter { set; get; }      
public List<MeasureDataReturnObject> data { set; get; }
public ErrorModel error { set; get; }
/*
[MeasureId]
      ,[CalendarId]
      ,[TargetId]
      ,[Value]
      ,[Explanation]
      ,[Action]
      ,[UserId]
      ,[LastUpdatedOn]
      ,[IsProcessed]
      */