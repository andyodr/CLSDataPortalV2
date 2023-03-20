// export interface Measure {
//     measureId?: string;
//     hierarchyId?: string;
//     measureDefinitionId?: string;
//     active?: string;
//     expression?: string;
//     rollup?: string;
//     owner?: string;
//     lastUpdatedOn?: string;
// }

export interface MeasureApiResponse {
    error: any
    hierarchy: string[]
    allow: boolean
    data: Data[]
  }
  
  export interface Data {
    id: number
    name: string
    owner: any
    hierarchy: Hierarchy[]
  }
  
  export interface Hierarchy {
    id: number
    active: boolean
    expression: boolean
    rollup: boolean
  }

  export interface MeasureApiParams{
    intervalId?: number;
    calendarId: number;
    year?: number;
    measureTypeId: number;
    hierarchyId: number; 
 
  }