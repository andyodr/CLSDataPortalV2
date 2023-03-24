import { Component, OnInit } from "@angular/core"
import { MatCheckboxChange } from "@angular/material/checkbox"
import { MatSelectChange } from "@angular/material/select"
import { ActivatedRoute } from "@angular/router"
import { Observable } from "rxjs"
import { Intervals } from "../lib/app-constants"
import { LoggerService } from "../_services/logger.service"
import {
    IntervalDto,
    MeasureDefinition, MeasureDefinitionEditDto, MeasureDefinitionService, MeasureType,
    Units
} from "../_services/measure-definition.service"

@Component({
    selector: "app-measuredefinition-edit",
    templateUrl: "./measuredefinition-edit.component.html",
    styleUrls: ["./measuredefinition-edit.component.scss"]
})
export class MeasureDefinitionEditComponent implements OnInit {
    title = "Add Measure Definition"
    roles: { id: any, name: any }[] = []
    hierarchy = []
    disabledAll = false
    measureTypes: MeasureType[] = []
    units: Units[] = []
    intervals: IntervalDto[] = []
    Intervals = Intervals
    aggFunctions: { id: number, name: string }[] = []
    dto!: MeasureDefinition
    md = {
        id: null as number | null,
        name: "",
        varName: "",
        measureType: null as MeasureType | null,
        description: "",
        precision: null as number | null,
        unit: null as Units | null,
        fieldNumber: null as number | null,
        expression: "",
        priority: null as number | null,
        interval: null as IntervalDto | null,
        weekly: false,
        monthly: false,
        quarterly: false,
        yearly: false,
        aggFunction: null as { id: number, name: string } | null
    }
    constructor(private route: ActivatedRoute, private api: MeasureDefinitionService, private logger: LoggerService) { }

    ngOnInit(): void {
        this.route.paramMap.subscribe(params => {
            const paramId = params.get("id")
            let api: Observable<MeasureDefinitionEditDto>
            if (paramId == null) {
                this.md.id = null
                api = this.api.getMeasureDefinitionEdit()
            }
            else {
                this.md.id = Number(paramId)
                this.title = "Edit Measure Definition"
                api = this.api.getMeasureDefinitionEdit(this.md.id)
            }

            api.subscribe(md => {
                this.measureTypes = md.measureTypes
                this.units = md.units
                this.intervals = md.intervals
                this.aggFunctions = md.aggFunctions
                if (this.md.id != null) {
                    this.dto = md.data[0]
                    this.refresh()
                }
            })
        })
    }

    intervalChanged(_: MatSelectChange) {
        if (this.md.id != null && this.md.interval?.id === this.dto.intervalId) {
            this.md.weekly = this.dto.weekly ?? false
            this.md.monthly = this.dto.monthly ?? false
            this.md.quarterly = this.dto.quarterly ?? false
            this.md.yearly = this.dto.yearly ?? false
        }
        else {
            this.md.weekly = this.md.monthly = this.md.quarterly = this.md.yearly = false
        }
    }

    cbChanged(_: MatCheckboxChange) {
        if (this.md.weekly || this.md.monthly || this.md.quarterly || this.md.yearly) {
            if (this.md.id != null && this.md.aggFunction == null) {
                // restore formControl model state from dto
                const aggFunction = this.aggFunctions.find(f => f.id === this.dto.aggFunctionId)
                this.md.aggFunction = aggFunction ?? this.aggFunctions[0]
            }
        }
        else {
            this.md.aggFunction = null
        }
    }

    refresh() {
        if (this.dto) {
            const { name, varName, measureTypeId, description, precision, unitId, fieldNumber, expression,
                priority, intervalId, weekly, monthly, quarterly, yearly, aggFunctionId } = this.dto
            const measureType = this.measureTypes.find(m => m.id === measureTypeId)
            const unit = this.units.find(u => u.id === unitId)
            const interval = this.intervals.find(n => n.id === intervalId)
            const aggFunction = this.aggFunctions.find(f => f.id === aggFunctionId)
            this.md.name = name
            this.md.varName = varName
            this.md.measureType = measureType ?? this.measureTypes[0]
            this.md.description = description ?? ""
            this.md.precision = precision
            this.md.unit = unit ?? this.units[0]
            this.md.fieldNumber = fieldNumber
            this.md.expression = expression ?? ""
            this.md.priority = priority
            this.md.interval = interval ?? this.intervals[0]
            this.md.weekly = weekly ?? false
            this.md.monthly = monthly ?? false
            this.md.quarterly = quarterly ?? false
            this.md.yearly = yearly ?? false
            this.md.aggFunction = aggFunction ?? this.aggFunctions[0]
            return true
        }

        return false
    }

    save() {
        const md = this.md
        if (md.measureType != null && md.interval != null && md.unit != null && md.aggFunction != null ) {
            const { name, measureType: { id: measureTypeId }, interval: { id: intervalId },
                varName, description, expression, unit: { id: unitId },
                weekly, monthly, quarterly, yearly, aggFunction: { id: aggFunctionId } } = md
            let { precision, priority, fieldNumber } = md
            precision ??= 0
            priority ??= 0
            fieldNumber ??= 0
            if (md.id != null) {
                const { id } = md
                this.api.updateMeasureDefinition(md.id, {
                    id, name, measureTypeId, intervalId, varName, description, expression, precision,
                    priority, fieldNumber, unitId, weekly, monthly, quarterly, yearly, aggFunctionId
                }).subscribe({
                    next: () => this.logger.logSuccess("saved")
                })
            }
            else {
                this.api.addMeasureDefinition({
                    name, measureTypeId, intervalId, varName, description, expression, precision,
                    priority, fieldNumber, unitId, weekly, monthly, quarterly, yearly, aggFunctionId
                }).subscribe({
                    next: () => this.logger.logSuccess("saved")
                })
            }
        }
        else {
            this.logger.logError("Missing parameters")
        }
    }
}
