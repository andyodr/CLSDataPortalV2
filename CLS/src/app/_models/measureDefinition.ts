//Interface for Measure Definition Model
export interface MeasureDefinition {
    measureDefinitionId?: string;
    measureTypeId?: string;
    reportIntervalId?: string;
    measureDefinitionName?: string;
    variableName?: string;
    description?: string;
    precision?: string;
    priority?: string;
    unitId?: string;
    calculated?: string;
    aggDaily?: string;
    aggWeekly?: string;
    aggMonthly?: string;
    aggQuarterly?: string;
    aggYearly?: string;
    aggFunction?: string;
    lastUpdatedOn?: string;
    isProcessed?: string;
    fieldNumber?: string;
    expression?: string;
}