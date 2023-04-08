import { RegionFilter } from "../_services/hierarchy.service";
import { ErrorModel } from "./error";

export interface TargetApiResponse {
  range?: string;
  calendarId?: number;
  allow: boolean;
  editValue: boolean;
  locked: boolean;
  confirmed: boolean;
  filter: TargetFilter;
  data: TargetDto[];
  error?: ErrorModel;
}

export interface TargetDto {
  id: number;
  name: string;
  value: number;
  explanation: string;
  action: string;
  target: number;
  targetCount: number;
  targetId: number;
  unitId: string;
  units: string;
  yellow: number;
  expression: string;
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

export interface TargetFilterResponseDto {
  measureTypes: MeasureType[]
  hierarchy: RegionFilter[]
  intervals: IntervalDto[]
  years: { id: number, year: number }[]
  error: ErrorModel
  filter: TargetFilter
  currentCalendarIds: CurrentCalendarIds
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

export interface TargetApiParams{
  intervalId?: number;
  calendarId?: number;
  year?: number;
  measureTypeId: number;
  hierarchyId: number; 
}

export interface ConfirmIntervals {
  daily: boolean
  weekly: boolean
  monthly: boolean
  quarterly: boolean
  yearly: boolean
}





 
  