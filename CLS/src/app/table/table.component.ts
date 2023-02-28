import { Component, Input } from '@angular/core';
import { SelCal } from '../app-constants';
import { FilterPipe } from '../filter.pipe';

@Component({
    selector: 'app-table',
    templateUrl: './table.component.html',
    styleUrls: ['./table.component.css'],
    providers: [FilterPipe]
})
export class TableComponent {
    @Input() data!: any[]
    colNames!: string[]
    searchKeywords = ""
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

    search(selCal: SelCal) {
        // All pages including Measures Data
        if (selCal == SelCal.All) {  
            this.filteredData = this.filterPipe.transform(this.data, this.searchKeywords)
        }
        else {  // This is only for Measures Data
            this.skMeasureData = ""

            if (selCal == SelCal.Calculated) {
                this.filteredData = this.data.filter(item => item.calculated == true)
            }
            else if (selCal == SelCal.Manual) {
                this.filteredData = this.data.filter(item => item.calculated == false)
            }
        }

        this.onFilterChange()
    }

    order(rowName: string) {
        if (this.row === rowName) {
            return
        }

        this.row = rowName
        if (this.filteredData == null) {
            this.filteredData = this.filterPipe.transform(this.data, rowName)
        }
        else {
            this.filteredData = this.filterPipe.transform(this.filteredData, rowName)
        }

        this.onOrderChange()
    }

    // formerly $broadcast: mmTableEvent, targetTableEvent, measuresTableEvent, hierarchyTableEvent
    // measureDefinitionTableEvent, dataImportsTableEvent, usersTableEvent, settingsTableEvent
    populate(searchKeywords?: string, selCal?: any) {
        if (searchKeywords != null) {
            this.searchKeywords = searchKeywords
        }

        this.search(selCal ?? SelCal.All)
    }

    indexTracking(index: number, _: any) {
        return index
    }
}
