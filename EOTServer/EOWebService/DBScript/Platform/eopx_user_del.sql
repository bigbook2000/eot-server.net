
-- set
UPDATE eox_user SET _update_flag=-1 WHERE f_user_id=#v_user_id;
-- end

-- set
select #v_user_id as f_user_id;
-- end