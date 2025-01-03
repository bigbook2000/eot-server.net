
---- 先删除版本
-- set
update n_dversion set _update_flag=-1 where f_dtype_id = #v_dtype_id;
-- end

-- set
update n_dtype set _update_flag=-1 where f_dtype_id = #v_dtype_id;
-- end
-- set
select 0 as _d, '' as _s, #v_dtype_id as f_dtype_id;
-- end