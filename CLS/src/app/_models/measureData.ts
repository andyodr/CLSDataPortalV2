import { RegionFilter } from "../_services/hierarchy.service";
import { ErrorModel } from "./error";

export interface MeasureDataApiResponse {
    range: string;
    calendarId: number;
    allow: boolean;
    editValue: boolean;
    locked: boolean;
    confirmed: boolean;
    filter: measureDataFilter;
    data: MeasureDataDto[];
    error?: ErrorModel;
}

export interface MeasureDataDto {
    id: number;
    //id: string;
    name: string;
    value: number;
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

export interface Updated {
    by: string;
    longDt: string;
    shortDt: string;
}

export interface measureDataFilter {
    intervalId: number;
    calendarId: number;
    year: number;
    measureTypeId: number;
    hierarchyId: number;
 }


export interface MeasureDataPutDto {
    calendarId: number;
    day: string;
    hierarchyId: number;
    measureTypeId: number;
    measureDataId: number;
    measureValue: number;
    explanation: string;
    action: string;
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

export type CurrentCalendarIds = {
    weeklyCalendarId: number
    monthlyCalendarId: number
    quarterlyCalendarId: number
    yearlyCalendarId: number
}

export type MeasureDataFilterResponseDto = {
    measureTypes: MeasureType[]
    hierarchy?: RegionFilter[]
    intervals?: IntervalDto[]
    years?: { id: number, year: number }[]
    error: ErrorModel
    currentCalendarIds?: CurrentCalendarIds
    filter: measureDataFilter
}

export type FiltersIntervalsData = {
    error?: ErrorModel
    id: number
    number?: number | null
    startDate?: string
    endDate?: string
    month?: string
    locked?: boolean
  }
  
  export type FiltersIntervalsDto = {
    calendarId: number
    data: FiltersIntervalsData[]
  }