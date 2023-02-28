import { Pipe, PipeTransform } from "@angular/core"

@Pipe({
    name: "filter"
})
export class FilterPipe implements PipeTransform {
    transform(items: string[], searchText: string) {
        if (!items) {
            return []
        }

        if (!searchText) {
            return items
        }

        searchText = searchText.toLocaleLowerCase()

        return items.filter(it => it.toLocaleLowerCase().includes(searchText))
    }
}
