import { ErrorModel } from "./error";
import { RegionFilter } from "./regionhierarchy";

// export interface Filter {
//     intervalId: number;
//     calendarId: number;
//     year: number;
//     measureTypeId: number;
//     hierarchyId: number;
// }

export interface measureDataFilter {
    intervalId: number;
    calendarId: number;
    year: number;
    measureTypeId: number;
    hierarchyId: number;
 }

export interface Updated {
    by: string;
    longDt: string;
    shortDt: string;
}

export interface MeasureDataDto {
    //id: number;
    id: string;
    name: string;
    value?: number;
    explanation?: any;
    action?: any;
    target?: any;
    targetCount?: any;
    targetId?: any;
    unitId: number;
    units: string;
    yellow?: any;
    expression: any;
    evaluated: string;
    calculated: boolean;
    description: string;
    updated: Updated;
}

export interface MeasureDataEditDto {
    calendarId: number;
    day: string;
    hierarchyId: number;
    measureTypeId: number;
    measureDataId: number;
    measureValue: number;
    explanation: string;
    action: string;
}

export interface MeasureDataResponse {
    range: string;
    calendarId: number;
    allow: boolean;
    editValue: boolean;
    locked: boolean;
    confirmed: boolean;
    filter: measureDataFilter;
    data: MeasureDataDto[];
    error?: any;
}

//--------------------------------------------

export type MeasureType = {
    id: number,
    name: string,
    description?: string
}

export type IntervalDto = {
    id: number
    name: string
}

export type CurrentCalendarIds = {
    weeklyCalendarId: number
    monthlyCalendarId: number
    quarterlyCalendarId: number
    yearlyCalendarId: number
}

export type FilterResponseDto = {
    measureTypes: MeasureType[]
    hierarchy?: RegionFilter[]
    intervals?: IntervalDto[]
    years?: { id: number, year: number }[]
    error: ErrorModel
    currentCalendarIds?: CurrentCalendarIds
    filter: measureDataFilter
}

// export type FilterResponseDto = {
//     measureTypes: MeasureType[]
//     hierarchy?: RegionFilter[]
//     intervals?: IntervalDto[]
//     years?: { id: number, year: number }[]
//     error: ErrorModel
//     currentCalendarIds?: CurrentCalendarIds
//     filter: {
//         hierarchyId?: number
//         measureTypeId?: number
//         intervalId?: number
//         calendarId?: number
//         year?: number
//     }
// }


// export type Units = { id: number, name: string, shortName: string }




//--------------------------------------------
//Interface for Measure Data Model
// export interface MeasureData {
//     measureDataId?: string;
//     measureId?: string;
//     calendarId?: string;
//     targetId?: string;
//     value?: string;
//     explanation?: string;
//     action?: string;
//     userId?: string;
//     lastUpdatedOn?: string;
//     isProcessed?: string
// }

// export interface MeasureDataIndexListObject{
//     range: string;
//     calendarId?: string;
//     allow?: boolean;
//     editValue?: boolean;
//     locked?: boolean;
//     confirmed?: boolean;
//     filter?: any;
//     data?: MeasureData[];
//     error?: string;
// } 

// export interface MeasureDataReceiveObject{
//     calendarId: number;
//     day: string;
//     hierarchyId: number;
//     measureTypeId: number;
//     //measureDataId { set; get; }      
//     //measureValue { set; get; }
//     explanation: string;
//     action: string;  
//     //public ErrorModel error { set; get; }
//   }