export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface PaginationParams {
  page?: number;
  pageSize?: number;
}
