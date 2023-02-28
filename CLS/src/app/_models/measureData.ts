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