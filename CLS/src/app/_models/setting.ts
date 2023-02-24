import { Time } from "@angular/common";

export interface Setting {
    id?: string;
    active?: string;
    lastCalculatedOn?: Date;
    lastUpdatedOn?: Date;
    calculateSchedule?: Time;
    numberOfDays?: number;
}
