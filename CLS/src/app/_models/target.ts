// export interface Target {
//     targetId?: string;
//     measureId?: string;
//     targetValue?: string;
//     yellowValue?: string;
//     active?: string;
//     lastUpdatedOn?: string;
//     userId?: string;
//     isProcessed?: string
// }

import { RegionFilter } from "../_services/hierarchy.service";
import { ErrorModel } from "./error";

// Path: CLS\src\app\_models\target.ts
  //--------------------------------------------------------------------------------
  //from GET api/targets/Index with hierarchyId and measureTypeId parameters
  //--------------------------------------------------------------------------------
  export interface TargetApiParams{
    intervalId?: number;
    calendarId?: number;
    year?: number;
    measureTypeId: number;
    hierarchyId: number; 
  }

export interface TargetApiResponse {
    range?: string;
    calendarId?: number;
    allow: boolean;
    editValue: boolean;
    locked: boolean;
    confirmed: boolean;
    filter: TargetFilter;
    data: TargetDto[];
    error?: any;
  }

  export type TargetFilter = {
    measureTypes: MeasureType[]
    hierarchy: RegionFilter[]
    intervals: any
    years: any
    error: any
    filter: Filter
    currentCalendarIds: any
    measures: TargetApiResponse
  }

  export type Filter = {
    hierarchyId?: number
    measureTypeId?: number
    intervalId?: number
    calendarId?: number
    year?: number
}

export interface TargetFilterResponseDto {
  measureTypes: MeasureType[]
  hierarchy: RegionFilter[]
  intervals: IntervalDto[]
  years: { id: number, year: number }[]
  error: ErrorModel
  filter: TargetFilter
  currentCalendarIds: CurrentCalendarIds
}

export type CurrentCalendarIds = {
  weeklyCalendarId: number
  monthlyCalendarId: number
  quarterlyCalendarId: number
  yearlyCalendarId: number
}
  
  export interface TargetDto {
    //id: number;
    id: string;
    name: string;
    value?: number;
    explanation?: string;
    action?: any;
    target?: any;
    targetCount?: any;
    targetId?: any;
    unitId?: number;
    units?: string;
    yellow?: any;
    expression?: any;
    evaluated?: string;
    calculated: boolean;
    description?: string;
    updated: Updated;
  }
  
  export interface Updated {
    by: string;
    longDt: string;
    shortDt: string;
  }

  // export interface TargetFilter {
  //   intervalId?: number;
  //   calendarId?: number;
  //   year?: number;
  //   measureTypeId: number;
  //   hierarchyId: number;
  // }
  




  export interface TargetPutDto {
    hierarchyId: number
    measureId?: number
    measureTypeId: number
    target: number
    yellow: number
    applyToChildren: boolean
    isCurrentUpdate: boolean
    confirmIntervals?: ConfirmIntervals
  }
  
  export interface ConfirmIntervals {
    daily: boolean
    weekly: boolean
    monthly: boolean
    quarterly: boolean
    yearly: boolean
  }

  //--------------------------------------------------------------------------------
  //from GET api/targets/ common for index and filter
  //--------------------------------------------------------------------------------

  //--------------------------------------------------------------------------------
  //from GET api/targets/Filter with no parameters
  //--------------------------------------------------------------------------------




  // export interface TargetFilter {
  //   hierarchies: any;
  //   measureTypes: MeasureType[]
  // }
  
  // export interface MeasureType {
  //   id: number
  //   name: string
  //   description?: string
  // }
  
  export interface Hierarchy {
    hierarchy: string
    id: number
    count: number
    sub: any[]
    found: any
    error: any
  }

  export type MeasureType = {
    id: number,
    name: string,
    description?: string
}

export type IntervalDto = {
    id: number
    name: string
}





export type FilterResponseDto = {
    measureTypes: MeasureType[]
    hierarchy?: RegionFilter[]
    intervals?: IntervalDto[]
    years?: { id: number, year: number }[]
    error: ErrorModel
    currentCalendarIds?: CurrentCalendarIds
    filter: Filter
}

export interface MeasureDefinition {
    id?: number
    name: string
    measureTypeId: number
    interval?: string
    intervalId: number
    varName: string
    description?: string
    expression?: string
    precision: number
    priority: number
    fieldNumber: number
    unitId: number
    units?: string
    calculated?: boolean
    daily?: boolean
    weekly?: boolean
    monthly?: boolean
    quarterly?: boolean
    yearly?: boolean
    aggFunction?: string
    aggFunctionId?: number
}

export type Units = { id: number, name: string, shortName: string }

export type MeasureDefinitionEditDto = {
    units: Units[]
    intervals: IntervalDto[]
    measureTypes: MeasureType[]
    aggFunctions: { id: number, name: string }[]
    data: MeasureDefinition[]
}


 
  