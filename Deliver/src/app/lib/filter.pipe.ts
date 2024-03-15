import { Injectable, Pipe, PipeTransform } from "@angular/core"

@Injectable({ providedIn: "root" })
@Pipe({
    name: "filter",
    standalone: true
})
export class FilterPipe implements PipeTransform {
    transform(items: any[], searchText: string) {
        if (!items) {
            return []
        }

        if (!searchText.trim()) {
            return items
        }

        searchText = searchText.toLocaleLowerCase()

        return items.filter(it => (it != null && (typeof it == "object" ?
            it.values().join("") :
            it.toString()).toLocaleLowerCase().includes(searchText)))
    }
}
