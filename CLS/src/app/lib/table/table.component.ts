import { Component, Input } from "@angular/core"
import { SelCal } from "../../app-constants"
import { FilterPipe } from "../../filter.pipe"

export type JsonValue = string | number | boolean | null

@Component({
    selector: "app-table",
    templateUrl: "./table.component.html",
    styleUrls: ["./table.component.css"],
    providers: [FilterPipe]
})
export class TableComponent {
    @Input() searchKeywords = ""
    dataSource!: { [name: string]: JsonValue }[]
    colNames!: string[]
    filteredData: any[] = []
    currentPageData: any[] = []
    row = ""
    numPerPageOpt = [3, 5, 10, 20]
    numPerPage = this.numPerPageOpt[0]
    currentPage = 1
    skMeasureData!: string

    public constructor(private filterPipe: FilterPipe) { }

    select() {
        // var end, start;
        // start = (page - 1) * vm.numPerPage;
        // end = start + vm.numPerPage;
        //return vm.currentPageData = vm.filteredData.slice(start, end);

        return this.currentPageData = this.filteredData
    }

    onFilterChange() {
        this.currentPageData = this.filteredData
        this.currentPage = 1
        this.row = ""
    }

    onNumPerPageChange() {
        this.currentPageData = this.filteredData
        this.currentPage = 1
    }

    onOrderChange() {
        this.currentPageData = this.filteredData
        this.currentPage = 1
    }

    populate(colNames: string[], dataSource: { [name: string]: JsonValue }[], selCal = SelCal.All) {
        // All pages including Measures Data
        if (selCal == SelCal.All) {  
            this.filteredData = this.filterPipe.transform(dataSource, this.searchKeywords)
        }
        else {  // This is only for Measures Data
            this.skMeasureData = ""

            if (selCal == SelCal.Calculated) {
                this.filteredData = dataSource.filter(item => item["calculated"] == true)
            }
            else if (selCal == SelCal.Manual) {
                this.filteredData = dataSource.filter(item => item["calculated"] == false)
            }
        }

        this.colNames = colNames
        this.dataSource = dataSource
        this.onFilterChange()
    }

    order(rowName: string) {
        if (this.row === rowName) {
            return
        }

        this.row = rowName
        if (this.filteredData == null) {
            this.filteredData = this.filterPipe.transform(this.dataSource, rowName)
        }
        else {
            this.filteredData = this.filterPipe.transform(this.filteredData, rowName)
        }

        this.onOrderChange()
    }

    indexTracking(index: number, _: any) {
        return index
    }
}
