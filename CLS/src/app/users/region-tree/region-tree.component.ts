import { SelectionModel } from "@angular/cdk/collections"
import { FlatTreeControl } from "@angular/cdk/tree"
import { Component, EventEmitter, Input, Output } from "@angular/core"
import { MatTreeFlatDataSource, MatTreeFlattener } from "@angular/material/tree"
import { RegionFilter, RegionFlatNode } from "../../_models/regionhierarchy"

@Component({
    selector: "app-region-tree",
    templateUrl: "./region-tree.component.html",
    styleUrls: ["./region-tree.component.scss"]
})
export class RegionTreeComponent {
    @Input()
    get hierarchy(): RegionFilter[] {
        return this._hierarchy
    }

    set hierarchy(hierarchy: RegionFilter[]) {
        this.treeData.data = this._hierarchy = hierarchy
        this.setInitialSelections(hierarchy, this.selectedRegions)
    }

    private _hierarchy!: RegionFilter[]

    @Input()
    get selectedRegions() {
        return [...this._selectedRegions.keys()]
    }

    set selectedRegions(list: number[]) {
        this._selectedRegions = new Set<number>(list)
        if (this.hierarchy) {
            this.setInitialSelections(this.hierarchy, list)
        }
    }

    private _selectedRegions = new Set<number>()
    @Output() selectedRegionsChange = new EventEmitter<number[]>()

    checklistSelection = new SelectionModel<RegionFlatNode>(true)
    flatNodeMap = new Map<RegionFlatNode, RegionFilter>()
    nestedNodeMap = new Map<RegionFilter, RegionFlatNode>()
    treeControl: FlatTreeControl<RegionFlatNode>
    treeFlattener!: MatTreeFlattener<RegionFilter, RegionFlatNode>
    treeData!: MatTreeFlatDataSource<RegionFilter, RegionFlatNode>
    constructor() {
        this.treeFlattener = new MatTreeFlattener(
            this.transformer,
            this.getLevel,
            this.isExpandable,
            this.getChildren
        )
        this.treeControl = new FlatTreeControl<RegionFlatNode>(this.getLevel, this.isExpandable)
        this.treeData = new MatTreeFlatDataSource(this.treeControl, this.treeFlattener)
        this.treeData.data = []
    }

    setInitialSelections(hierarchy: RegionFilter[], selectedRegions: number[]): void {
        if (selectedRegions.length > 0) {
            let n = 0
            let fh = [hierarchy[0]]  // flattened version of hierarchy
            while (n < fh.length) {
                if (Array.isArray(fh[n].sub) && fh[n].sub.length > 0) {
                    fh = fh.concat(...fh[n].sub)
                }

                n++
            }

            let m = new Map<number, RegionFilter>(fh.map(rf => [rf.id, rf]))
            let initiallySelectedRegions = selectedRegions
                .map(id => m.get(id)).filter((rf): rf is RegionFilter => !!rf)
                .map(rf => this.nestedNodeMap.get(rf)).filter((fn): fn is RegionFlatNode => !!fn)
            //this.checklistSelection = new SelectionModel<RegionFlatNode>(true, initiallySelectedRegions)
            this.checklistSelection.select(...initiallySelectedRegions)
        }
    }

    getLevel = (node: RegionFlatNode): number => node.level
    isExpandable = (node: RegionFlatNode): boolean => node.expandable
    getChildren = (node: RegionFilter): RegionFilter[] => node.sub
    hasChild = (_: number, _nodeData: RegionFlatNode): boolean => _nodeData.expandable
    hasNoContent = (_: number, _nodeData: RegionFlatNode): boolean => _nodeData.hierarchy === ""
    transformer = (node: RegionFilter, level: number): RegionFlatNode => {
        const prevNode = this.nestedNodeMap.get(node)
        const flatNode = prevNode?.hierarchy === node.hierarchy ? prevNode : new RegionFlatNode()
        flatNode.hierarchy = node.hierarchy
        flatNode.level = level
        flatNode.expandable = !!node.sub?.length
        this.flatNodeMap.set(flatNode, node)
        this.nestedNodeMap.set(node, flatNode)
        return flatNode
    }

    descendantsAllSelected(node: RegionFlatNode): boolean {
        const descendants = this.treeControl.getDescendants(node)
        const descAllSelected =
            descendants.length > 0 &&
            descendants.every(child => {
                return this.checklistSelection.isSelected(child)
            })
        return descAllSelected
    }

    descendantsPartiallySelected(node: RegionFlatNode): boolean {
        const descendants = this.treeControl.getDescendants(node)
        const result = descendants.some(child => this.checklistSelection.isSelected(child))
        return result && !this.descendantsAllSelected(node)
    }

    getParentNode(node: RegionFlatNode): RegionFlatNode | undefined {
        const currentLevel = this.getLevel(node)
        if (currentLevel < 1) {
            return
        }

        const startIndex = this.treeControl.dataNodes.indexOf(node) - 1
        for (let i = startIndex; i >= 0; i--) {
            const currentNode = this.treeControl.dataNodes[i]

            if (this.getLevel(currentNode) < currentLevel) {
                return currentNode
            }
        }

        return
    }

    updateRootNodeSelection(node: RegionFlatNode): void {
        const nodeSelected = this.checklistSelection.isSelected(node)
        const descendants = this.treeControl.getDescendants(node)
        const descAllSelected =
            descendants.length > 0 &&
            descendants.every(child => {
                return this.checklistSelection.isSelected(child)
            })
        if (nodeSelected && !descAllSelected) {
            this.checklistSelection.deselect(node)
            this.removeSelected(node)
        }
        else if (!nodeSelected && descAllSelected) {
            this.checklistSelection.select(node)
            this.addSelected(node)
        }
    }

    checkAllParentsSelection(node: RegionFlatNode): void {
        let parent: RegionFlatNode | undefined = this.getParentNode(node)
        while (parent) {
            this.updateRootNodeSelection(parent)
            parent = this.getParentNode(parent)
        }
    }

    regionSelectionToggle(node: RegionFlatNode): void {
        this.checklistSelection.toggle(node)
        const descendants = this.treeControl.getDescendants(node)
        if (this.checklistSelection.isSelected(node)) {
            this.checklistSelection.select(...descendants)
            this.addSelected(node, ...descendants)
        }
        else {
            this.checklistSelection.deselect(...descendants)
            this.removeSelected(node, ...descendants)
        }

        // Force update for the parent
        descendants.forEach(child => this.checklistSelection.isSelected(child))
        this.checkAllParentsSelection(node)
    }

    addSelected(...nodes: RegionFlatNode[]) {
        for (let node of nodes) {
            let rh = this.flatNodeMap.get(node)
            if (rh !== undefined) {
                this._selectedRegions.add(rh.id)
            }
        }

        this.selectedRegionsChange.emit(this.selectedRegions)
    }

    removeSelected(...nodes: RegionFlatNode[]) {
        for (let node of nodes) {
            let rh = this.flatNodeMap.get(node)
            if (rh !== undefined) {
                this._selectedRegions.delete(rh.id)
            }
        }

        this.selectedRegionsChange.emit(this.selectedRegions)
    }

    reset() {
        this.checklistSelection.clear()
    }
}
