//casegroup.swift
import Foundation
import RealityKit

struct CaseGroup: Identifiable, Hashable {
    let id: UUID
    let usdzModelNames: [String]
    let name: String
    let description: String
}

struct LoadedCaseGroup: Identifiable {
    let id: UUID
    let group: CaseGroup
    let usdzURLs: [URL]
    let usdzEntities: [Entity?]
}